using RecurPixel.Notify.Core.Models;

namespace RecurPixel.Notify.Core.Channels;

/// <summary>
/// Core contract for all notification channel adapters.
/// Every adapter — email, SMS, push, Slack, etc. — implements this interface.
/// Adapters are unaware of orchestration, fallback, or retry logic.
/// Their sole responsibility is: accept a payload, attempt delivery, return a result.
/// </summary>
public interface INotificationChannel
{
    /// <summary>
    /// The unique name identifying this channel.
    /// Must be lowercase kebab-case. e.g. "email", "sms", "push", "whatsapp".
    /// Used by the orchestrator to match event channel config to adapters.
    /// </summary>
    string ChannelName { get; }

    /// <summary>
    /// Sends a notification to a single recipient.
    /// All exceptions must be caught internally and returned as NotifyResult { Success = false }.
    /// This method must never throw.
    /// </summary>
    /// <param name="payload">The notification content and destination.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the send attempt.</returns>
    Task<NotifyResult> SendAsync(NotificationPayload payload, CancellationToken ct = default);

    /// <summary>
    /// Sends a notification to multiple recipients in one operation.
    /// Adapters with native batch APIs override this for efficiency.
    /// Adapters without native batch APIs inherit the default loop from NotificationChannelBase.
    /// </summary>
    /// <param name="payloads">The list of payloads to deliver.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A bulk result containing one NotifyResult per input payload.</returns>
    Task<BulkNotifyResult> SendBulkAsync(IReadOnlyList<NotificationPayload> payloads, CancellationToken ct = default);
}