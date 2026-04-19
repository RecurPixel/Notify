namespace RecurPixel.Notify;

/// <summary>
/// Persistence contract for the notification log.
/// Implement this interface to use a custom storage backend (Dapper, MongoDB, etc.).
/// For an EF Core implementation, install <c>RecurPixel.Notify.Dashboard.EfCore</c>
/// and call <c>AddNotifyDashboardEfCore()</c>.
/// </summary>
public interface INotificationLogStore
{
    /// <summary>
    /// Persists a single <see cref="NotificationLog"/> entry.
    /// Called automatically after every send attempt when the dashboard is registered.
    /// </summary>
    /// <param name="log">The log entry to store.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(NotificationLog log, CancellationToken ct = default);

    /// <summary>
    /// Returns a paged, filtered list of log entries ordered by <see cref="NotificationLog.SentAt"/> descending.
    /// </summary>
    /// <param name="query">Filter and paging parameters. All fields are optional.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<NotificationLog>> QueryAsync(NotificationLogQuery query, CancellationToken ct = default);

    /// <summary>
    /// Returns all log entries belonging to a single bulk send, ordered by <see cref="NotificationLog.SentAt"/>.
    /// </summary>
    /// <param name="bulkBatchId">The batch ID from <see cref="NotifyResult.BulkBatchId"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<NotificationLog>> GetBatchAsync(string bulkBatchId, CancellationToken ct = default);

    /// <summary>
    /// Returns summary statistics for today (UTC).
    /// Used by the dashboard UI summary row.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<NotificationLogStats> GetTodayStatsAsync(CancellationToken ct = default);
}
