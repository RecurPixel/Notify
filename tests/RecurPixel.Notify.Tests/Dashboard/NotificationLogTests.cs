namespace RecurPixel.Notify.Tests.Dashboard;

public class NotificationLogTests
{
    // ── FromResult mapping ────────────────────────────────────────────────────

    [Fact]
    public void FromResult_MapsAllFields()
    {
        var sentAt = new DateTime(2026, 4, 19, 10, 30, 0, DateTimeKind.Utc);
        var result = new NotifyResult
        {
            Channel       = "email",
            Provider      = "sendgrid",
            Recipient     = "a@b.com",
            Subject       = "Hello",
            EventName     = "order.placed",
            Success       = true,
            ProviderId    = "msg_123",
            Error         = null,
            BulkBatchId   = "batch-abc",
            UsedFallback  = false,
            NamedProvider = "primary",
            SentAt        = sentAt
        };

        var log = NotificationLog.FromResult(result);

        Assert.Equal("email",       log.Channel);
        Assert.Equal("sendgrid",    log.Provider);
        Assert.Equal("a@b.com",     log.Recipient);
        Assert.Equal("Hello",       log.Subject);
        Assert.Equal("order.placed",log.EventName);
        Assert.True(log.Success);
        Assert.Equal("msg_123",     log.ProviderId);
        Assert.Null(log.Error);
        Assert.True(log.IsBulk);
        Assert.Equal("batch-abc",   log.BulkBatchId);
        Assert.False(log.UsedFallback);
        Assert.Equal("primary",     log.NamedProvider);
        Assert.Equal(sentAt,        log.SentAt);
    }

    [Fact]
    public void FromResult_IsBulk_TrueWhenBulkBatchIdSet()
    {
        var result = new NotifyResult { BulkBatchId = "some-batch", SentAt = DateTime.UtcNow };
        Assert.True(NotificationLog.FromResult(result).IsBulk);
    }

    [Fact]
    public void FromResult_IsBulk_FalseWhenBulkBatchIdNull()
    {
        var result = new NotifyResult { BulkBatchId = null, SentAt = DateTime.UtcNow };
        Assert.False(NotificationLog.FromResult(result).IsBulk);
    }

    [Fact]
    public void FromResult_SentAt_DefaultReplacedWithUtcNow()
    {
        var result = new NotifyResult { SentAt = default };
        var log = NotificationLog.FromResult(result);
        Assert.NotEqual(default, log.SentAt);
        Assert.True((DateTime.UtcNow - log.SentAt).TotalSeconds < 5);
    }

    [Fact]
    public void FromResult_NullRecipient_MapsToEmpty()
    {
        var result = new NotifyResult { Recipient = null, SentAt = DateTime.UtcNow };
        Assert.Equal(string.Empty, NotificationLog.FromResult(result).Recipient);
    }

    [Fact]
    public void FromResult_FailureResult_MapsErrorAndSuccess()
    {
        var result = new NotifyResult
        {
            Success = false,
            Error   = "Connection refused",
            Channel = "sms",
            SentAt  = DateTime.UtcNow
        };
        var log = NotificationLog.FromResult(result);
        Assert.False(log.Success);
        Assert.Equal("Connection refused", log.Error);
    }

    // ── NotificationLogStats ──────────────────────────────────────────────────

    [Theory]
    [InlineData(10, 8, 80.0)]
    [InlineData(0,  0, 0.0)]
    [InlineData(3,  1, 33.3)]
    public void Stats_SuccessRate_CalculatedCorrectly(int total, int succeeded, double expected)
    {
        var stats = new NotificationLogStats
        {
            TotalSent    = total,
            SuccessCount = succeeded,
            FailureCount = total - succeeded
        };
        Assert.Equal(expected, stats.SuccessRate);
    }

    [Fact]
    public void Stats_SuccessRate_ZeroWhenNoSends()
    {
        var stats = new NotificationLogStats { TotalSent = 0 };
        Assert.Equal(0, stats.SuccessRate);
    }
}
