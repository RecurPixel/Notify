namespace RecurPixel.Notify.Core.Models;

/// <summary>
/// The full context passed to TriggerAsync or BulkTriggerAsync.
/// Combines the recipient user with the per-channel payloads for this notification.
/// </summary>
public class NotifyContext
{
    /// <summary>
    /// The recipient of this notification.
    /// Used for condition evaluation and logging.
    /// </summary>
    public NotifyUser User { get; set; } = new();

    /// <summary>
    /// Per-channel payloads keyed by channel name.
    /// Key must match a registered channel name e.g. "email", "sms", "push".
    /// Only channels present in this dictionary will be attempted.
    /// </summary>
    public Dictionary<string, NotificationPayload> Channels { get; set; } = new();
}