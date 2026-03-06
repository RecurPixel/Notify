using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify.Channels;

/// <summary>
/// Notification channel adapter for in-app / inbox delivery.
/// Invokes the handler registered via <c>OnDeliver</c> on every send.
/// Storage, SignalR, queuing, and persistence are entirely user-owned.
/// </summary>
[ChannelAdapter("inapp", "default")]
public sealed class InAppChannel : NotificationChannelBase
{
    private readonly InAppOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InAppChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "inapp";

    /// <summary>
    /// Initialises a new instance of <see cref="InAppChannel"/>.
    /// </summary>
    public InAppChannel(
        IOptions<InAppOptions> options,
        IServiceProvider serviceProvider,
        ILogger<InAppChannel> logger)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "InApp: delivering notification to recipient {Recipient}",
            payload.To);

        if (_options.DeliverHandler is null)
        {
            const string error =
                "InApp delivery handler not configured. " +
                "Call AddInAppChannel(inApp => inApp.OnDeliver(...)) to register a handler.";

            _logger.LogDebug(
                "InApp: no handler configured for recipient {Recipient}",
                payload.To);

            return Fail(payload, error);
        }

        var notification = new InAppNotification
        {
            UserId = payload.To,
            Subject = payload.Subject,
            Body = payload.Body,
            Metadata = payload.Metadata
        };

        try
        {
            var result = await _options.DeliverHandler(notification, _serviceProvider);

            // Ensure the result always carries channel identity and recipient
            result.Channel = ChannelName;
            result.Provider = "inapp";
            result.Recipient = payload.To;

            if (result.SentAt == default)
                result.SentAt = DateTime.UtcNow;

            if (result.Success)
                _logger.LogDebug(
                    "InApp: notification delivered to recipient {Recipient}",
                    payload.To);
            else
                _logger.LogDebug(
                    "InApp: handler returned failure for recipient {Recipient}. Error={Error}",
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

    private NotifyResult Fail(NotificationPayload payload, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "inapp",
        Recipient = payload.To,
        Error = error,
        SentAt = DateTime.UtcNow
    };
}
