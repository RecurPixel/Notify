using Microsoft.EntityFrameworkCore;

namespace RecurPixel.Notify.Dashboard.EfCore;

/// <summary>
/// EF Core implementation of <see cref="INotificationLogStore"/>.
/// Registered by <c>AddNotifyDashboardEfCore()</c>. Uses <see cref="NotifyDashboardDbContext"/>
/// for Option A (standalone context) or your own DbContext for Option B.
/// </summary>
public sealed class EfCoreNotificationLogStore : INotificationLogStore
{
    private readonly NotifyDashboardDbContext _db;

    /// <summary>Initialises the store with the provided DbContext.</summary>
    public EfCoreNotificationLogStore(NotifyDashboardDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task AddAsync(NotificationLog log, CancellationToken ct = default)
    {
        _db.NotificationLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<NotificationLog>> QueryAsync(
        NotificationLogQuery query,
        CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 500);
        var page     = Math.Max(query.Page, 1);

        var q = _db.NotificationLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(query.Channel))
            q = q.Where(l => l.Channel == query.Channel);

        if (!string.IsNullOrEmpty(query.Provider))
            q = q.Where(l => l.Provider == query.Provider);

        if (query.Success.HasValue)
            q = q.Where(l => l.Success == query.Success.Value);

        if (query.From.HasValue)
            q = q.Where(l => l.SentAt >= query.From.Value);

        if (query.To.HasValue)
            q = q.Where(l => l.SentAt <= query.To.Value);

        if (!string.IsNullOrEmpty(query.Recipient))
            q = q.Where(l => l.Recipient.Contains(query.Recipient));

        if (!string.IsNullOrEmpty(query.EventName))
            q = q.Where(l => l.EventName == query.EventName);

        if (!string.IsNullOrEmpty(query.BulkBatchId))
            q = q.Where(l => l.BulkBatchId == query.BulkBatchId);

        if (query.IsBulk.HasValue)
            q = q.Where(l => l.IsBulk == query.IsBulk.Value);

        var results = await q
            .OrderByDescending(l => l.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return results;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<NotificationLog>> GetBatchAsync(
        string bulkBatchId,
        CancellationToken ct = default)
    {
        return await _db.NotificationLogs
            .AsNoTracking()
            .Where(l => l.BulkBatchId == bulkBatchId)
            .OrderBy(l => l.SentAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<NotificationLogStats> GetTodayStatsAsync(CancellationToken ct = default)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        var logs = await _db.NotificationLogs
            .AsNoTracking()
            .Where(l => l.SentAt >= todayUtc && l.SentAt < tomorrowUtc)
            .Select(l => new { l.Success, l.Channel })
            .ToListAsync(ct);

        return new NotificationLogStats
        {
            TotalSent          = logs.Count,
            SuccessCount       = logs.Count(l => l.Success),
            FailureCount       = logs.Count(l => !l.Success),
            ActiveChannelCount = logs.Select(l => l.Channel).Distinct().Count()
        };
    }
}
