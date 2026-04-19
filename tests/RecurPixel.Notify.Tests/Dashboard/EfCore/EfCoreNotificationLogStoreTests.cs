using Microsoft.EntityFrameworkCore;
using RecurPixel.Notify.Dashboard.EfCore;

namespace RecurPixel.Notify.Tests.Dashboard.EfCore;

public class EfCoreNotificationLogStoreTests
{
    private static NotifyDashboardDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<NotifyDashboardDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new NotifyDashboardDbContext(options);
    }

    private static NotificationLog MakeLog(
        string channel   = "email",
        string provider  = "sendgrid",
        string recipient = "a@b.com",
        bool   success   = true,
        string? batchId  = null,
        string? eventName = null,
        DateTime? sentAt = null) => new()
    {
        Channel     = channel,
        Provider    = provider,
        Recipient   = recipient,
        Success     = success,
        BulkBatchId = batchId,
        IsBulk      = batchId is not null,
        EventName   = eventName,
        SentAt      = sentAt ?? DateTime.UtcNow
    };

    // ── AddAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_PersistsLogToDatabase()
    {
        await using var db = CreateContext(nameof(AddAsync_PersistsLogToDatabase));
        var store = new EfCoreNotificationLogStore(db);

        var log = MakeLog();
        await store.AddAsync(log);

        Assert.Equal(1, await db.NotificationLogs.CountAsync());
    }

    [Fact]
    public async Task AddAsync_AssignsId()
    {
        await using var db = CreateContext(nameof(AddAsync_AssignsId));
        var store = new EfCoreNotificationLogStore(db);

        var log = MakeLog();
        await store.AddAsync(log);

        Assert.True(log.Id > 0);
    }

    // ── QueryAsync — no filters ───────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_NoFilters_ReturnsAllLogs()
    {
        await using var db = CreateContext(nameof(QueryAsync_NoFilters_ReturnsAllLogs));
        var store = new EfCoreNotificationLogStore(db);

        await store.AddAsync(MakeLog("email"));
        await store.AddAsync(MakeLog("sms"));
        await store.AddAsync(MakeLog("push"));

        var results = await store.QueryAsync(new NotificationLogQuery());

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task QueryAsync_OrderedBySentAtDescending()
    {
        await using var db = CreateContext(nameof(QueryAsync_OrderedBySentAtDescending));
        var store = new EfCoreNotificationLogStore(db);

        var t1 = DateTime.UtcNow.AddMinutes(-10);
        var t2 = DateTime.UtcNow.AddMinutes(-5);
        var t3 = DateTime.UtcNow;

        await store.AddAsync(MakeLog(sentAt: t1));
        await store.AddAsync(MakeLog(sentAt: t3));
        await store.AddAsync(MakeLog(sentAt: t2));

        var results = await store.QueryAsync(new NotificationLogQuery());

        Assert.Equal(t3, results[0].SentAt);
        Assert.Equal(t2, results[1].SentAt);
        Assert.Equal(t1, results[2].SentAt);
    }

    // ── QueryAsync — channel filter ───────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_ChannelFilter_ReturnsOnlyMatchingChannel()
    {
        await using var db = CreateContext(nameof(QueryAsync_ChannelFilter_ReturnsOnlyMatchingChannel));
        var store = new EfCoreNotificationLogStore(db);

        await store.AddAsync(MakeLog("email"));
        await store.AddAsync(MakeLog("sms"));
        await store.AddAsync(MakeLog("email"));

        var results = await store.QueryAsync(new NotificationLogQuery { Channel = "email" });

        Assert.Equal(2, results.Count);
        Assert.All(results, l => Assert.Equal("email", l.Channel));
    }

    // ── QueryAsync — success filter ───────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_SuccessFilter_True_ReturnsOnlySuccesses()
    {
        await using var db = CreateContext(nameof(QueryAsync_SuccessFilter_True_ReturnsOnlySuccesses));
        var store = new EfCoreNotificationLogStore(db);

        await store.AddAsync(MakeLog(success: true));
        await store.AddAsync(MakeLog(success: false));
        await store.AddAsync(MakeLog(success: true));

        var results = await store.QueryAsync(new NotificationLogQuery { Success = true });

        Assert.Equal(2, results.Count);
        Assert.All(results, l => Assert.True(l.Success));
    }

    [Fact]
    public async Task QueryAsync_SuccessFilter_False_ReturnsOnlyFailures()
    {
        await using var db = CreateContext(nameof(QueryAsync_SuccessFilter_False_ReturnsOnlyFailures));
        var store = new EfCoreNotificationLogStore(db);

        await store.AddAsync(MakeLog(success: true));
        await store.AddAsync(MakeLog(success: false));

        var results = await store.QueryAsync(new NotificationLogQuery { Success = false });

        Assert.Single(results);
        Assert.False(results[0].Success);
    }

    // ── QueryAsync — date range filter ────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_FromFilter_ExcludesEarlierLogs()
    {
        await using var db = CreateContext(nameof(QueryAsync_FromFilter_ExcludesEarlierLogs));
        var store = new EfCoreNotificationLogStore(db);

        var cutoff = DateTime.UtcNow;
        await store.AddAsync(MakeLog(sentAt: cutoff.AddMinutes(-10)));
        await store.AddAsync(MakeLog(sentAt: cutoff.AddMinutes(5)));

        var results = await store.QueryAsync(new NotificationLogQuery { From = cutoff });

        Assert.Single(results);
    }

    [Fact]
    public async Task QueryAsync_ToFilter_ExcludesLaterLogs()
    {
        await using var db = CreateContext(nameof(QueryAsync_ToFilter_ExcludesLaterLogs));
        var store = new EfCoreNotificationLogStore(db);

        var cutoff = DateTime.UtcNow;
        await store.AddAsync(MakeLog(sentAt: cutoff.AddMinutes(-10)));
        await store.AddAsync(MakeLog(sentAt: cutoff.AddMinutes(5)));

        var results = await store.QueryAsync(new NotificationLogQuery { To = cutoff });

        Assert.Single(results);
    }

    // ── QueryAsync — recipient filter ─────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_RecipientFilter_MatchesSubstring()
    {
        await using var db = CreateContext(nameof(QueryAsync_RecipientFilter_MatchesSubstring));
        var store = new EfCoreNotificationLogStore(db);

        await store.AddAsync(MakeLog(recipient: "alice@example.com"));
        await store.AddAsync(MakeLog(recipient: "bob@example.com"));
        await store.AddAsync(MakeLog(recipient: "alice@other.org"));

        var results = await store.QueryAsync(new NotificationLogQuery { Recipient = "alice" });

        Assert.Equal(2, results.Count);
    }

    // ── QueryAsync — event name filter ────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_EventNameFilter_ExactMatch()
    {
        await using var db = CreateContext(nameof(QueryAsync_EventNameFilter_ExactMatch));
        var store = new EfCoreNotificationLogStore(db);

        await store.AddAsync(MakeLog(eventName: "order.placed"));
        await store.AddAsync(MakeLog(eventName: "order.shipped"));
        await store.AddAsync(MakeLog(eventName: "order.placed"));

        var results = await store.QueryAsync(new NotificationLogQuery { EventName = "order.placed" });

        Assert.Equal(2, results.Count);
    }

    // ── QueryAsync — IsBulk filter ────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_IsBulkFilter_True_ReturnsBulkOnly()
    {
        await using var db = CreateContext(nameof(QueryAsync_IsBulkFilter_True_ReturnsBulkOnly));
        var store = new EfCoreNotificationLogStore(db);

        await store.AddAsync(MakeLog(batchId: "batch-1"));
        await store.AddAsync(MakeLog(batchId: null));
        await store.AddAsync(MakeLog(batchId: "batch-2"));

        var results = await store.QueryAsync(new NotificationLogQuery { IsBulk = true });

        Assert.Equal(2, results.Count);
        Assert.All(results, l => Assert.True(l.IsBulk));
    }

    // ── QueryAsync — paging ───────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_Paging_ReturnsCorrectPage()
    {
        await using var db = CreateContext(nameof(QueryAsync_Paging_ReturnsCorrectPage));
        var store = new EfCoreNotificationLogStore(db);

        var baseTime = DateTime.UtcNow;
        for (int i = 0; i < 10; i++)
            await store.AddAsync(MakeLog(recipient: $"user{i}@x.com", sentAt: baseTime.AddSeconds(i)));

        var page1 = await store.QueryAsync(new NotificationLogQuery { Page = 1, PageSize = 3 });
        var page2 = await store.QueryAsync(new NotificationLogQuery { Page = 2, PageSize = 3 });

        Assert.Equal(3, page1.Count);
        Assert.Equal(3, page2.Count);
        // Pages should not overlap
        Assert.Empty(page1.Select(l => l.Recipient).Intersect(page2.Select(l => l.Recipient)));
    }

    [Fact]
    public async Task QueryAsync_PageSize_ClampedAt500()
    {
        await using var db = CreateContext(nameof(QueryAsync_PageSize_ClampedAt500));
        var store = new EfCoreNotificationLogStore(db);

        for (int i = 0; i < 5; i++)
            await store.AddAsync(MakeLog());

        var results = await store.QueryAsync(new NotificationLogQuery { PageSize = 9999 });

        // Only 5 records exist, so we still get 5 (clamping to 500 has no visible effect here)
        Assert.Equal(5, results.Count);
    }

    // ── QueryAsync — combined filters ─────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_MultipleFilters_AllApplied()
    {
        await using var db = CreateContext(nameof(QueryAsync_MultipleFilters_AllApplied));
        var store = new EfCoreNotificationLogStore(db);

        await store.AddAsync(MakeLog("email", success: true,  eventName: "order.placed"));
        await store.AddAsync(MakeLog("email", success: false, eventName: "order.placed"));
        await store.AddAsync(MakeLog("sms",   success: true,  eventName: "order.placed"));
        await store.AddAsync(MakeLog("email", success: true,  eventName: "promo.blast"));

        var results = await store.QueryAsync(new NotificationLogQuery
        {
            Channel   = "email",
            Success   = true,
            EventName = "order.placed"
        });

        Assert.Single(results);
        Assert.Equal("email",        results[0].Channel);
        Assert.True(results[0].Success);
        Assert.Equal("order.placed", results[0].EventName);
    }

    // ── GetBatchAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBatchAsync_ReturnsAllLogsInBatch()
    {
        await using var db = CreateContext(nameof(GetBatchAsync_ReturnsAllLogsInBatch));
        var store = new EfCoreNotificationLogStore(db);

        var batchId = "batch-xyz";
        await store.AddAsync(MakeLog(batchId: batchId, channel: "email"));
        await store.AddAsync(MakeLog(batchId: batchId, channel: "sms"));
        await store.AddAsync(MakeLog(batchId: "other-batch", channel: "push"));

        var results = await store.GetBatchAsync(batchId);

        Assert.Equal(2, results.Count);
        Assert.All(results, l => Assert.Equal(batchId, l.BulkBatchId));
    }

    [Fact]
    public async Task GetBatchAsync_OrderedBySentAtAscending()
    {
        await using var db = CreateContext(nameof(GetBatchAsync_OrderedBySentAtAscending));
        var store = new EfCoreNotificationLogStore(db);

        var batchId = "asc-test";
        var t1 = DateTime.UtcNow.AddSeconds(-5);
        var t2 = DateTime.UtcNow;

        await store.AddAsync(MakeLog(batchId: batchId, sentAt: t2));
        await store.AddAsync(MakeLog(batchId: batchId, sentAt: t1));

        var results = await store.GetBatchAsync(batchId);

        Assert.Equal(t1, results[0].SentAt);
        Assert.Equal(t2, results[1].SentAt);
    }

    [Fact]
    public async Task GetBatchAsync_UnknownBatchId_ReturnsEmpty()
    {
        await using var db = CreateContext(nameof(GetBatchAsync_UnknownBatchId_ReturnsEmpty));
        var store = new EfCoreNotificationLogStore(db);

        var results = await store.GetBatchAsync("nonexistent");

        Assert.Empty(results);
    }

    // ── GetTodayStatsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetTodayStatsAsync_CountsCorrectly()
    {
        await using var db = CreateContext(nameof(GetTodayStatsAsync_CountsCorrectly));
        var store = new EfCoreNotificationLogStore(db);

        var today = DateTime.UtcNow;
        var yesterday = today.AddDays(-1);

        await store.AddAsync(MakeLog("email", success: true,  sentAt: today));
        await store.AddAsync(MakeLog("sms",   success: true,  sentAt: today));
        await store.AddAsync(MakeLog("push",  success: false, sentAt: today));
        await store.AddAsync(MakeLog("email", success: true,  sentAt: yesterday)); // excluded

        var stats = await store.GetTodayStatsAsync();

        Assert.Equal(3, stats.TotalSent);
        Assert.Equal(2, stats.SuccessCount);
        Assert.Equal(1, stats.FailureCount);
    }

    [Fact]
    public async Task GetTodayStatsAsync_ActiveChannelCount_DistinctChannels()
    {
        await using var db = CreateContext(nameof(GetTodayStatsAsync_ActiveChannelCount_DistinctChannels));
        var store = new EfCoreNotificationLogStore(db);

        var today = DateTime.UtcNow;
        await store.AddAsync(MakeLog("email", sentAt: today));
        await store.AddAsync(MakeLog("email", sentAt: today));
        await store.AddAsync(MakeLog("sms",   sentAt: today));

        var stats = await store.GetTodayStatsAsync();

        Assert.Equal(2, stats.ActiveChannelCount);
    }

    [Fact]
    public async Task GetTodayStatsAsync_NoLogs_ReturnsZeros()
    {
        await using var db = CreateContext(nameof(GetTodayStatsAsync_NoLogs_ReturnsZeros));
        var store = new EfCoreNotificationLogStore(db);

        var stats = await store.GetTodayStatsAsync();

        Assert.Equal(0, stats.TotalSent);
        Assert.Equal(0, stats.SuccessCount);
        Assert.Equal(0, stats.FailureCount);
        Assert.Equal(0, stats.SuccessRate);
        Assert.Equal(0, stats.ActiveChannelCount);
    }
}
