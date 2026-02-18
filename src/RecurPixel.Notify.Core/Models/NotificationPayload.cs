namespace RecurPixel.Notify.Core.Models;

/// <summary>
/// Represents the data to be delivered through a notification channel.
/// The caller owns all content — subject, body, and metadata are passed through as-is.
/// </summary>
public class NotificationPayload
{
    /// <summary>
    /// The destination address for this notification.
    /// Interpretation depends on the channel:
    /// email address, phone number, device token, webhook URL, chat ID, etc.
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// The notification title or subject.
    /// Used for email subject lines and push notification titles.
    /// Optional for channels that have no subject concept (SMS, WhatsApp).
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// The notification body content.
    /// Can be plain text or HTML depending on the channel and provider.
    /// The library does not render or modify this value.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Channel-specific extras that do not fit the standard payload shape.
    /// Examples: provider name for routing, FCM data payload, Slack blocks, etc.
    /// Key "provider" is reserved for named provider routing.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}