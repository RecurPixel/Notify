using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Channels;

namespace RecurPixel.Notify.Orchestrator.Dispatch;

/// <summary>
/// Resolves the correct channel adapter for a given payload and dispatches the send.
/// Handles named-provider routing, within-channel fallback, retry with exponential backoff,
/// and the delivery hook.
/// Multi-provider resolution order:
///   1. Metadata["provider"] → named routing → NamedProviderDefinition.Type
///   2. Default Channel.Provider
///   3. On failure after retries → Channel.Fallback (or NamedProviderDefinition.Fallback)
/// </summary>
internal sealed class ChannelDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly NotifyOptions _notifyOptions;
    private readonly ILogger<ChannelDispatcher> _logger;

    public ChannelDispatcher(
        IServiceProvider serviceProvider,
        IOptions<NotifyOptions> notifyOptions,
        ILogger<ChannelDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _notifyOptions   = notifyOptions.Value;
        _logger          = logger;
    }

    /// <summary>
    /// Dispatches a single payload to the correct provider adapter for the given channel.
    /// Retries on failure according to <paramref name="retryOptions"/>. When null, a single
    /// attempt is made with no retry. Returns a failed <see cref="NotifyResult"/> on exception
    /// — never throws.
    /// </summary>
    public async Task<NotifyResult> DispatchAsync(
        string channelName,
        NotificationPayload payload,
        RetryOptions? retryOptions,
        CancellationToken ct)
    {
        try
        {
            var (primaryKey, fallbackKey, namedProviderName) = ResolveKeys(channelName, payload);

            _logger.LogDebug(
                "Dispatching channel={Channel} provider={ProviderKey} named={Named}",
                channelName, primaryKey, namedProviderName ?? "(default)");

            var adapter = ResolveAdapter(primaryKey, channelName);
            var result  = await SendWithRetryAsync(adapter, payload, retryOptions, ct);

            result.Channel       = channelName;
            result.NamedProvider = namedProviderName;

            if (result.Success)
                return result;

            // Primary failed after all retries — try within-channel fallback provider (single attempt, no retry)
            if (fallbackKey is not null)
            {
                _logger.LogDebug(
                    "Primary provider failed for channel={Channel}, trying within-channel fallback={FallbackKey}",
                    channelName, fallbackKey);

                var fallbackAdapter = ResolveAdapter(fallbackKey, channelName);
                var fallbackResult  = await SendAsync(fallbackAdapter, payload, ct);

                fallbackResult.Channel       = channelName;
                fallbackResult.NamedProvider = namedProviderName;
                fallbackResult.UsedFallback  = true;

                return fallbackResult;
            }

            return result;
        }
        catch (InvalidOperationException)
        {
            // Re-throw config errors — these are loud failures by design
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unhandled exception dispatching channel={Channel}", channelName);
            return new NotifyResult
            {
                Success = false,
                Channel = channelName,
                Error   = ex.Message,
                SentAt  = DateTime.UtcNow
            };
        }
    }

    // ── Retry ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a payload to the given adapter, retrying on failure according to
    /// <paramref name="retryOptions"/>. When <paramref name="retryOptions"/> is null,
    /// a single attempt is made with no retry.
    /// Exponential backoff: attempt 1 waits DelayMs, attempt 2 waits DelayMs×2,
    /// attempt 3 waits DelayMs×4, and so on.
    /// </summary>
    private async Task<NotifyResult> SendWithRetryAsync(
        INotificationChannel adapter,
        NotificationPayload payload,
        RetryOptions? retryOptions,
        CancellationToken ct)
    {
        var maxAttempts = retryOptions?.MaxAttempts ?? 1;
        var delayMs     = retryOptions?.DelayMs ?? 500;
        var exponential = retryOptions?.ExponentialBackoff ?? true;

        NotifyResult lastResult = null!;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            lastResult = await SendAsync(adapter, payload, ct);

            if (lastResult.Success)
                return lastResult;

            if (attempt < maxAttempts)
            {
                // Delay doubles each attempt when exponential backoff is enabled.
                // Attempt 1 → DelayMs, attempt 2 → DelayMs×2, attempt 3 → DelayMs×4, …
                var waitMs = exponential
                    ? delayMs * (int)Math.Pow(2, attempt - 1)
                    : delayMs;

                _logger.LogDebug(
                    "Attempt {Attempt}/{MaxAttempts} failed for channel={Channel}. Retrying in {DelayMs}ms.",
                    attempt, maxAttempts, adapter.ChannelName, waitMs);

                await Task.Delay(waitMs, ct);
            }
        }

        return lastResult;
    }

    // ── Key resolution ────────────────────────────────────────────────────────

    private (string primaryKey, string? fallbackKey, string? namedProviderName)
        ResolveKeys(string channelName, NotificationPayload payload)
    {
        var (defaultProvider, channelFallback, namedProviders) = GetChannelConfig(channelName);

        // Check for named routing via Metadata["provider"]
        string? namedProviderName = null;
        if (payload.Metadata is not null &&
            payload.Metadata.TryGetValue("provider", out var providerObj) &&
            providerObj is string providerName &&
            !string.IsNullOrWhiteSpace(providerName))
        {
            namedProviderName = providerName;

            if (namedProviders is null || !namedProviders.TryGetValue(providerName, out var namedDef))
                throw new InvalidOperationException(
                    $"Named provider '{providerName}' for channel '{channelName}' is not configured. " +
                    $"Add it to Notify:{Capitalize(channelName)}:Providers in your options.");

            var namedFallbackKey = namedDef.Fallback is not null
                ? $"{channelName}:{namedDef.Fallback}"
                : null;

            return ($"{channelName}:{namedDef.Type}", namedFallbackKey, namedProviderName);
        }

        // No named routing — use default provider
        if (string.IsNullOrWhiteSpace(defaultProvider))
        {
            // Simple channel with no provider selection (Slack, Discord, Teams, etc.)
            return (channelName, null, null);
        }

        var fallbackKey = channelFallback is not null ? $"{channelName}:{channelFallback}" : null;
        return ($"{channelName}:{defaultProvider}", fallbackKey, null);
    }

    private (string? defaultProvider, string? fallback, Dictionary<string, NamedProviderDefinition>? namedProviders)
        GetChannelConfig(string channelName) => channelName switch
    {
        "email"    => (_notifyOptions.Email?.Provider,    _notifyOptions.Email?.Fallback,    _notifyOptions.Email?.Providers),
        "sms"      => (_notifyOptions.Sms?.Provider,      _notifyOptions.Sms?.Fallback,      _notifyOptions.Sms?.Providers),
        "push"     => (_notifyOptions.Push?.Provider,     _notifyOptions.Push?.Fallback,     _notifyOptions.Push?.Providers),
        "whatsapp" => (_notifyOptions.WhatsApp?.Provider, _notifyOptions.WhatsApp?.Fallback, _notifyOptions.WhatsApp?.Providers),
        _          => (null, null, null)   // simple channels — registered by channel name only
    };

    private INotificationChannel ResolveAdapter(string key, string channelName)
    {
        var adapter = _serviceProvider.GetKeyedService<INotificationChannel>(key);
        if (adapter is null)
            throw new InvalidOperationException(
                $"No adapter registered for key '{key}'. " +
                $"Ensure the provider package for channel '{channelName}' is installed and registered.");
        return adapter;
    }

    private static async Task<NotifyResult> SendAsync(
        INotificationChannel adapter,
        NotificationPayload payload,
        CancellationToken ct)
    {
        try
        {
            return await adapter.SendAsync(payload, ct);
        }
        catch (Exception ex)
        {
            return new NotifyResult
            {
                Success  = false,
                Channel  = adapter.ChannelName,
                Provider = adapter.ChannelName,
                Error    = ex.Message,
                SentAt   = DateTime.UtcNow
            };
        }
    }

    private static string Capitalize(string s) =>
        s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
