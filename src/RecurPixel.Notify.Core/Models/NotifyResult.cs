namespace RecurPixel.Notify.Core.Models;

/// <summary>
/// Represents the outcome of a single notification send attempt.
/// Returned by every SendAsync call regardless of channel or provider.
/// </summary>
public class NotifyResult
{
    /// <summary>
    /// True if the provider accepted the notification for delivery.
    /// False if any error occurred — check Error for details.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The channel that attempted delivery. e.g. "email", "sms", "push".
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// The provider that handled the send. e.g. "sendgrid", "twilio", "fcm".
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// The named provider used for routing, if Metadata["provider"] was set.
    /// Null if the default provider was used.
    /// </summary>
    public string? NamedProvider { get; set; }

    /// <summary>
    /// True if the send succeeded only after falling back to a secondary provider.
    /// </summary>
    public bool UsedFallback { get; set; }

    /// <summary>
    /// The provider's own message ID, useful for delivery tracking and support queries.
    /// Null if the provider does not return one or if the send failed.
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Error message if Success is false. Null if the send succeeded.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// UTC timestamp of when the send was attempted.
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// The recipient identifier for this result.
    /// Set automatically from NotificationPayload.To in bulk operations.
    /// Allows failed bulk results to be traced back to specific recipients.
    /// </summary>
    public string? Recipient { get; set; }
}