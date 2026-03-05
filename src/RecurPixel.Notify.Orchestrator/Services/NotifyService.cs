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
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotifyService> _logger;

    public NotifyService(
        EventRegistry registry,
        ChannelDispatcher dispatcher,
        OrchestratorOptions orchOptions,
        IOptions<NotifyOptions> notifyOptions,
        IServiceProvider serviceProvider,
        ILogger<NotifyService> logger)
    {
        _registry        = registry;
        _dispatcher      = dispatcher;
        _orchOptions     = orchOptions;
        _notifyOptions   = notifyOptions.Value;
        _serviceProvider = serviceProvider;
        _logger          = logger;
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
    public INotificationChannel InApp       => new RoutingChannel("inapp",       _dispatcher);
    /// <inheritdoc/>
    public INotificationChannel Line        => new RoutingChannel("line",        _dispatcher);
    /// <inheritdoc/>
    public INotificationChannel Viber       => new RoutingChannel("viber",       _dispatcher);
    /// <inheritdoc/>
    public INotificationChannel Mattermost  => new RoutingChannel("mattermost",  _dispatcher);
    /// <inheritdoc/>
    public INotificationChannel RocketChat  => new RoutingChannel("rocketchat",  _dispatcher);

    // ── Orchestrated single send ──────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<TriggerResult> TriggerAsync(
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
        var retryOptions = eventDef.Retry ?? _notifyOptions.Retry;

        // Evaluate conditions — skip channels where condition returns false
        var activeChannels = eventDef.Channels
            .Where(ch =>
                !eventDef.Conditions.TryGetValue(ch, out var condition) || condition(context))
            .ToList();

        _logger.LogDebug("Event={Event} active channels after conditions: {Active}",
            eventName, string.Join(",", activeChannels));

        var channelResults = new List<NotifyResult>();
        var dispatchedResults = new List<NotifyResult>(); // only actual dispatch attempts — drives the hook

        // Dispatch each active channel, logging a warning when no payload is provided
        foreach (var channelName in activeChannels)
        {
            if (context.Channels is null || !context.Channels.ContainsKey(channelName))
            {
                _logger.LogWarning(
                    "Event '{EventName}' targets channel '{Channel}' but no payload was provided " +
                    "in NotifyContext.Channels. No notification sent. " +
                    "Ensure UseChannels() uses logical names (e.g. \"email\", not \"email:sendgrid\").",
                    eventName, channelName);

                channelResults.Add(new NotifyResult
                {
                    Success = false,
                    Channel = channelName,
                    Error   = $"No payload for channel '{channelName}' in NotifyContext.Channels.",
                    SentAt  = DateTime.UtcNow
                });
                continue;
            }

            var dispatchResult = await _dispatcher.DispatchAsync(channelName, ResolvePayload(context.Channels[channelName], channelName, context.User), retryOptions, ct);
            channelResults.Add(dispatchResult);
            dispatchedResults.Add(dispatchResult);
        }

        if (channelResults.Count == 0)
        {
            return new TriggerResult
            {
                EventName      = eventName,
                UserId         = context.User.UserId,
                ChannelResults = []
            };
        }

        // Fire delivery hook only for actual dispatch attempts (not missing-payload skips)
        await InvokeDeliveryHookAsync(dispatchedResults);

        // Cross-channel fallback: only when ALL primary dispatches failed and a chain is defined
        var dispatched = activeChannels
            .Where(ch => context.Channels?.ContainsKey(ch) == true)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (eventDef.FallbackChain is { Count: > 0 } && channelResults.All(r => !r.Success))
        {
            _logger.LogDebug(
                "All primary channels failed for event={Event}. Attempting cross-channel fallback chain.",
                eventName);

            var fallbackResult = await TryFallbackChainAsync(
                eventDef.FallbackChain,
                dispatched,
                context,
                retryOptions,
                ct);

            if (fallbackResult is not null)
                channelResults.Add(fallbackResult);
        }

        return new TriggerResult
        {
            EventName      = eventName,
            UserId         = context.User.UserId,
            ChannelResults = channelResults
        };
    }

    // ── Orchestrated bulk send ────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<BulkTriggerResult> BulkTriggerAsync(
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

        return new BulkTriggerResult { Results = results };
    }

    // ── Cross-channel fallback ────────────────────────────────────────────────

    /// <summary>
    /// Tries channels in <paramref name="chain"/> in order, skipping any already dispatched.
    /// Stops and returns the first successful result with <see cref="NotifyResult.UsedFallback"/> set.
    /// Fires the delivery hook for every attempt. Returns <c>null</c> if all fail.
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
            if (alreadyDispatched.Contains(channel))
            {
                _logger.LogDebug("Fallback chain: skipping channel={Channel} — already attempted.", channel);
                continue;
            }

            if (context.Channels is null || !context.Channels.TryGetValue(channel, out var payload))
            {
                _logger.LogDebug("Fallback chain: skipping channel={Channel} — no payload in context.", channel);
                continue;
            }

            _logger.LogDebug("Fallback chain: trying channel={Channel}.", channel);

            var result = await _dispatcher.DispatchAsync(channel, ResolvePayload(payload, channel, context.User), retryOptions, ct);
            result.UsedFallback = true;

            await InvokeHookSafe(result);

            if (result.Success)
            {
                _logger.LogDebug("Fallback chain: channel={Channel} succeeded.", channel);
                return result;
            }

            _logger.LogDebug("Fallback chain: channel={Channel} failed — trying next.", channel);
        }

        return null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a payload with <see cref="NotificationPayload.To"/> populated from
    /// <paramref name="user"/> when the caller left it empty.
    /// Creates a copy — never mutates the caller's original object.
    /// Mapping: email→User.Email, sms/whatsapp→User.Phone, push→User.DeviceToken, inapp→User.UserId.
    /// </summary>
    private static NotificationPayload ResolvePayload(
        NotificationPayload payload,
        string channel,
        NotifyUser user)
    {
        if (!string.IsNullOrEmpty(payload.To))
            return payload;

        var autoTo = channel switch
        {
            "email"    => user.Email,
            "sms"      => user.Phone,
            "push"     => user.DeviceToken,
            "whatsapp" => user.Phone,
            "inapp"    => string.IsNullOrEmpty(user.UserId) ? null : user.UserId,
            _          => null
        };

        if (string.IsNullOrEmpty(autoTo))
            return payload;

        return new NotificationPayload
        {
            To       = autoTo,
            Subject  = payload.Subject,
            Body     = payload.Body,
            Metadata = payload.Metadata
        };
    }

    private async Task InvokeDeliveryHookAsync(IEnumerable<NotifyResult> results)
    {
        if (!_orchOptions.HasDeliveryHandlers) return;

        var hookTasks = results.Select(r => InvokeHookSafe(r));
        await Task.WhenAll(hookTasks);
    }

    private async Task InvokeHookSafe(NotifyResult result)
    {
        if (!_orchOptions.HasDeliveryHandlers) return;
        try
        {
            await _orchOptions.InvokeDeliveryHandlers(result, _serviceProvider);
        }
        catch (Exception ex)
        {
            // Hook failures must never crash the dispatch path
            _logger.LogDebug(ex, "OnDelivery hook threw for channel={Channel}", result.Channel);
        }
    }
}
