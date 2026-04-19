namespace RecurPixel.Notify;

/// <summary>
/// Persisted record of a single notification send attempt.
/// Written automatically by the dashboard's <see cref="INotifyDeliveryObserver"/> after
/// every send — both successes and failures are stored.
/// </summary>
public class NotificationLog
{
    /// <summary>Auto-incremented primary key.</summary>
    public long Id { get; set; }

    /// <summary>The channel used for delivery. e.g. "email", "sms", "push".</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>The provider that handled the send. e.g. "sendgrid", "twilio", "fcm".</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>The recipient identifier — email address, phone number, device token etc.</summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// The notification subject (email subject, push title etc.).
    /// Null for channels that have no subject or when sent via direct channel call.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>The event name that triggered this notification. Null for direct channel sends.</summary>
    public string? EventName { get; set; }

    /// <summary>True if the provider accepted the notification for delivery.</summary>
    public bool Success { get; set; }

    /// <summary>The provider's own message ID for delivery tracking. Null if unavailable or send failed.</summary>
    public string? ProviderId { get; set; }

    /// <summary>Error message when Success is false. Null on success.</summary>
    public string? Error { get; set; }

    /// <summary>True when this log belongs to a bulk send (BulkBatchId is not null).</summary>
    public bool IsBulk { get; set; }

    /// <summary>
    /// Groups all log entries from one <c>BulkTriggerAsync</c> call.
    /// Use this to retrieve all recipients in a single bulk send via
    /// <see cref="INotificationLogStore.GetBatchAsync"/>.
    /// </summary>
    public string? BulkBatchId { get; set; }

    /// <summary>True when the send succeeded only after the Orchestrator tried a fallback channel.</summary>
    public bool UsedFallback { get; set; }

    /// <summary>The named provider used for routing. Null when the default provider was used.</summary>
    public string? NamedProvider { get; set; }

    /// <summary>UTC timestamp of when the send was attempted.</summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Creates a <see cref="NotificationLog"/> from a <see cref="NotifyResult"/>.
    /// <c>IsBulk</c> is derived from whether <see cref="NotifyResult.BulkBatchId"/> is set.
    /// </summary>
    public static NotificationLog FromResult(NotifyResult result) => new()
    {
        Channel       = result.Channel,
        Provider      = result.Provider,
        Recipient     = result.Recipient ?? string.Empty,
        Subject       = result.Subject,
        EventName     = result.EventName,
        Success       = result.Success,
        ProviderId    = result.ProviderId,
        Error         = result.Error,
        IsBulk        = result.BulkBatchId is not null,
        BulkBatchId   = result.BulkBatchId,
        UsedFallback  = result.UsedFallback,
        NamedProvider = result.NamedProvider,
        SentAt        = result.SentAt == default ? DateTime.UtcNow : result.SentAt,
    };
}
