namespace RecurPixel.Notify;

/// <summary>
/// Filter and paging parameters for querying the notification log.
/// All filter fields are optional — omitted fields match all records.
/// </summary>
public class NotificationLogQuery
{
    /// <summary>Filter by channel name. e.g. "email", "sms", "push". Null matches all channels.</summary>
    public string? Channel { get; set; }

    /// <summary>Filter by provider name. e.g. "sendgrid", "twilio". Null matches all providers.</summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Filter by delivery outcome.
    /// <c>true</c> = successes only, <c>false</c> = failures only, <c>null</c> = all.
    /// </summary>
    public bool? Success { get; set; }

    /// <summary>Filter to logs sent at or after this UTC timestamp. Null = no lower bound.</summary>
    public DateTime? From { get; set; }

    /// <summary>Filter to logs sent at or before this UTC timestamp. Null = no upper bound.</summary>
    public DateTime? To { get; set; }

    /// <summary>
    /// Filter by recipient — partial, case-insensitive substring match.
    /// Null = all recipients.
    /// </summary>
    public string? Recipient { get; set; }

    /// <summary>Filter by event name — exact, case-insensitive match. Null = all events.</summary>
    public string? EventName { get; set; }

    /// <summary>Filter to all logs belonging to a specific bulk batch. Null = all batches.</summary>
    public string? BulkBatchId { get; set; }

    /// <summary>Filter to only bulk sends (<c>true</c>), only single sends (<c>false</c>), or all (<c>null</c>).</summary>
    public bool? IsBulk { get; set; }

    /// <summary>1-based page number. Defaults to 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Number of records per page. Defaults to 50. Max enforced by the store implementation.</summary>
    public int PageSize { get; set; } = 50;
}
