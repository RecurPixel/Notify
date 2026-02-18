using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;

namespace RecurPixel.Notify.InApp;

/// <summary>
/// Notification channel adapter for in-app / inbox delivery.
/// Invokes a user-provided delegate on every send — storage, SignalR,
/// queuing, and persistence are entirely user-owned.
/// </summary>
public sealed class InAppChannel : NotificationChannelBase
{
    private readonly InAppOptions _options;
    private readonly ILogger<InAppChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "inapp";

    /// <summary>
    /// Initialises a new instance of <see cref="InAppChannel"/>.
    /// </summary>
    public InAppChannel(
        IOptions<InAppOptions> options,
        ILogger<InAppChannel> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    /// <inheritdoc />
    public override async Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "InApp: delivering notification to recipient {Recipient}",
            payload.To);

        if (_options.Handler is null)
        {
            const string error = "InApp channel is not configured. " +
                                 "Set InAppOptions.Handler via AddRecurPixelNotify().";

            _logger.LogDebug(
                "InApp: no handler configured for recipient {Recipient}",
                payload.To);

            return Fail(payload, error);
        }

        try
        {
            var result = await _options.Handler(payload, ct);

            // Ensure the result always carries channel identity and recipient
            // regardless of what the user delegate returned.
            result.Channel   = ChannelName;
            result.Provider  = "inapp";
            result.Recipient = payload.To;

            if (result.SentAt == default)
                result.SentAt = DateTime.UtcNow;

            if (result.Success)
                _logger.LogDebug(
                    "InApp: notification delivered to recipient {Recipient}",
                    payload.To);
            else
                _logger.LogDebug(
                    "InApp: handler returned failure for recipient {Recipient}. Error {Error}",
                    payload.To, result.Error);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "InApp: exception in handler for recipient {Recipient}",
                payload.To);

            return Fail(payload, ex.Message);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private NotifyResult Fail(NotificationPayload payload, string error) => new()
    {
        Success   = false,
        Channel   = ChannelName,
        Provider  = "inapp",
        Recipient = payload.To,
        Error     = error,
        SentAt    = DateTime.UtcNow
    };
}
