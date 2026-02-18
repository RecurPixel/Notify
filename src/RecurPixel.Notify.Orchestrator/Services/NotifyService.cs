using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Orchestrator.Dispatch;
using RecurPixel.Notify.Orchestrator.Events;
using RecurPixel.Notify.Orchestrator.Options;

namespace RecurPixel.Notify.Orchestrator.Services;

/// <summary>
/// Full implementation of <see cref="INotifyService"/>.
/// Registered as scoped by <c>AddRecurPixelNotifyOrchestrator()</c>.
/// </summary>
internal sealed class NotifyService : INotifyService
{
    private readonly EventRegistry _registry;
    private readonly ChannelDispatcher _dispatcher;
    private readonly OrchestratorOptions _orchOptions;
    private readonly NotifyOptions _notifyOptions;
    private readonly ILogger<NotifyService> _logger;

    public NotifyService(
        EventRegistry registry,
        ChannelDispatcher dispatcher,
        OrchestratorOptions orchOptions,
        IOptions<NotifyOptions> notifyOptions,
        ILogger<NotifyService> logger)
    {
        _registry      = registry;
        _dispatcher    = dispatcher;
        _orchOptions   = orchOptions;
        _notifyOptions = notifyOptions.Value;
        _logger        = logger;
    }

    // ── Direct channel properties ─────────────────────────────────────────────

    /// <inheritdoc/>
    public INotificationChannel Email    => new RoutingChannel("email",    _dispatcher);
    /// <inheritdoc/>
    public INotificationChannel Sms      => new RoutingChannel("sms",      _dispatcher);
    /// <inheritdoc/>
    public INotificationChannel Push     => new RoutingChannel("push",     _dispatcher);
    /// <inheritdoc/>
    public INotificationChannel WhatsApp => new RoutingChannel("whatsapp", _dispatcher);
    /// <inheritdoc/>
    public INotificationChannel Slack    => new RoutingChannel("slack",    _dispatcher);
    /// <inheritdoc/>
    public INotificationChannel Discord  => new RoutingChannel("discord",  _dispatcher);
    /// <inheritdoc/>
    public INotificationChannel Teams    => new RoutingChannel("teams",    _dispatcher);
    /// <inheritdoc/>
    public INotificationChannel Telegram => new RoutingChannel("telegram", _dispatcher);
    /// <inheritdoc/>
    public INotificationChannel Facebook => new RoutingChannel("facebook", _dispatcher);
    /// <inheritdoc/>
    public INotificationChannel InApp    => new RoutingChannel("inapp",    _dispatcher);

    // ── Orchestrated single send ──────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<NotifyResult> TriggerAsync(
        string eventName,
        NotifyContext context,
        CancellationToken ct = default)
    {
        var eventDef = _registry.Get(eventName)
            ?? throw new InvalidOperationException(
                $"Event '{eventName}' is not defined. Register it via DefineEvent() in AddRecurPixelNotifyOrchestrator().");

        _logger.LogDebug("Triggering event={Event} channels={Channels}",
            eventName, string.Join(",", eventDef.Channels));

        // Per-event retry takes precedence over global retry.
        // If neither is set, ChannelDispatcher defaults to a single attempt.
        var retryOptions = eventDef.Retry ?? _notifyOptions.Retry;

        // Evaluate conditions — skip channels where condition returns false
        var activeChannels = eventDef.Channels
            .Where(ch =>
                !eventDef.Conditions.TryGetValue(ch, out var condition) || condition(context))
            .ToList();

        _logger.LogDebug("Event={Event} active channels after conditions: {Active}",
            eventName, string.Join(",", activeChannels));

        // Only dispatch channels that have a payload defined in context
        var channelsToDispatch = activeChannels
            .Where(ch => context.Channels is not null && context.Channels.ContainsKey(ch))
            .ToList();

        if (channelsToDispatch.Count == 0)
        {
            return new NotifyResult
            {
                Success = true,
                Channel = eventName,
                SentAt  = DateTime.UtcNow
            };
        }

        var dispatchTasks = channelsToDispatch
            .Select(ch => _dispatcher.DispatchAsync(ch, context.Channels![ch], retryOptions, ct))
            .ToList();

        var results = await Task.WhenAll(dispatchTasks);

        // Fire delivery hook for each primary channel result
        await InvokeDeliveryHookAsync(results);

        // Cross-channel fallback: only when ALL primary channels failed and a chain is defined
        if (eventDef.FallbackChain is { Count: > 0 } && results.All(r => !r.Success))
        {
            _logger.LogDebug(
                "All primary channels failed for event={Event}. Attempting cross-channel fallback chain.",
                eventName);

            var fallbackResult = await TryFallbackChainAsync(
                eventDef.FallbackChain,
                new HashSet<string>(channelsToDispatch, StringComparer.OrdinalIgnoreCase),
                context,
                retryOptions,
                ct);

            if (fallbackResult is not null)
                return fallbackResult;
        }

        return BuildAggregateResult(eventName, results);
    }

    // ── Orchestrated bulk send ────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<BulkNotifyResult> BulkTriggerAsync(
        string eventName,
        IReadOnlyList<NotifyContext> contexts,
        CancellationToken ct = default)
    {
        var concurrencyLimit = _notifyOptions.Bulk?.ConcurrencyLimit ?? 10;
        var semaphore = new SemaphoreSlim(concurrencyLimit);

        _logger.LogDebug(
            "BulkTrigger event={Event} contexts={Count} concurrency={Limit}",
            eventName, contexts.Count, concurrencyLimit);

        var tasks = contexts.Select(async ctx =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                return await TriggerAsync(eventName, ctx, ct);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        return new BulkNotifyResult
        {
            Results         = results,
            Channel         = eventName,
            UsedNativeBatch = false
        };
    }

    // ── Cross-channel fallback ────────────────────────────────────────────────

    /// <summary>
    /// Tries channels in <paramref name="chain"/> in order, skipping any channel already
    /// dispatched in the primary pass. Stops and returns the first successful result.
    /// Sets <see cref="NotifyResult.UsedFallback"/> to <c>true</c> on the returned result.
    /// Fires the delivery hook for every attempt (success or failure).
    /// Returns <c>null</c> if every channel in the chain also fails or has no payload.
    /// </summary>
    private async Task<NotifyResult?> TryFallbackChainAsync(
        IReadOnlyList<string> chain,
        HashSet<string> alreadyDispatched,
        NotifyContext context,
        RetryOptions? retryOptions,
        CancellationToken ct)
    {
        foreach (var channel in chain)
        {
            // Skip channels already attempted in the primary pass
            if (alreadyDispatched.Contains(channel))
            {
                _logger.LogDebug(
                    "Fallback chain: skipping channel={Channel} — already attempted.", channel);
                continue;
            }

            // Skip channels with no payload in context
            if (context.Channels is null || !context.Channels.TryGetValue(channel, out var payload))
            {
                _logger.LogDebug(
                    "Fallback chain: skipping channel={Channel} — no payload in context.", channel);
                continue;
            }

            _logger.LogDebug("Fallback chain: trying channel={Channel}.", channel);

            var result = await _dispatcher.DispatchAsync(channel, payload, retryOptions, ct);
            result.UsedFallback = true;

            // Fire hook for this fallback attempt regardless of outcome
            await InvokeHookSafe(result);

            if (result.Success)
            {
                _logger.LogDebug("Fallback chain: channel={Channel} succeeded.", channel);
                return result;
            }

            _logger.LogDebug(
                "Fallback chain: channel={Channel} failed — trying next.", channel);
        }

        return null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task InvokeDeliveryHookAsync(IEnumerable<NotifyResult> results)
    {
        if (_orchOptions.DeliveryHook is null) return;

        var hookTasks = results.Select(r => InvokeHookSafe(r));
        await Task.WhenAll(hookTasks);
    }

    private async Task InvokeHookSafe(NotifyResult result)
    {
        if (_orchOptions.DeliveryHook is null) return;
        try
        {
            await _orchOptions.DeliveryHook(result);
        }
        catch (Exception ex)
        {
            // Hook failures must never crash the dispatch path
            _logger.LogDebug(ex, "OnDelivery hook threw for channel={Channel}", result.Channel);
        }
    }

    private static NotifyResult BuildAggregateResult(string eventName, NotifyResult[] results)
    {
        var allSucceeded = results.All(r => r.Success);
        var channels     = string.Join(",", results.Select(r => r.Channel));
        var errors       = string.Join("; ", results.Where(r => !r.Success).Select(r => r.Error));

        return new NotifyResult
        {
            Success = allSucceeded,
            Channel = channels,
            Error   = allSucceeded ? null : errors,
            SentAt  = DateTime.UtcNow
        };
    }
}
