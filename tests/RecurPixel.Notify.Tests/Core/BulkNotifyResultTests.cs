namespace RecurPixel.Notify.Tests.Core;

public class BulkNotifyResultTests
{
    private static NotifyResult Success(string recipient) => new()
    {
        Success = true,
        Channel = "fake",
        Recipient = recipient,
        SentAt = DateTime.UtcNow
    };

    private static NotifyResult Failure(string recipient, string error) => new()
    {
        Success = false,
        Channel = "fake",
        Recipient = recipient,
        Error = error,
        SentAt = DateTime.UtcNow
    };

    [Fact]
    public void AllSucceeded_True_WhenEveryResultSucceeds()
    {
        var bulk = new BulkNotifyResult
        {
            Results = new List<NotifyResult> { Success("a@x.com"), Success("b@x.com") },
            Channel = "fake",
            UsedNativeBatch = false
        };

        Assert.True(bulk.AllSucceeded);
    }

    [Fact]
    public void AllSucceeded_False_WhenAnyResultFails()
    {
        var bulk = new BulkNotifyResult
        {
            Results = new List<NotifyResult> { Success("a@x.com"), Failure("b@x.com", "timeout") },
            Channel = "fake",
            UsedNativeBatch = false
        };

        Assert.False(bulk.AllSucceeded);
    }

    [Fact]
    public void AnySucceeded_True_WhenAtLeastOneSucceeds()
    {
        var bulk = new BulkNotifyResult
        {
            Results = new List<NotifyResult> { Success("a@x.com"), Failure("b@x.com", "timeout") },
            Channel = "fake",
            UsedNativeBatch = false
        };

        Assert.True(bulk.AnySucceeded);
    }

    [Fact]
    public void Failures_ReturnsOnlyFailedResults()
    {
        var bulk = new BulkNotifyResult
        {
            Results = new List<NotifyResult>
            {
                Success("a@x.com"),
                Failure("b@x.com", "timeout"),
                Failure("c@x.com", "invalid address")
            },
            Channel = "fake",
            UsedNativeBatch = false
        };

        Assert.Equal(2, bulk.FailureCount);
        Assert.Equal(2, bulk.Failures.Count);
        Assert.Equal(1, bulk.SuccessCount);
    }

    [Fact]
    public void Total_MatchesResultsCount()
    {
        var bulk = new BulkNotifyResult
        {
            Results = new List<NotifyResult> { Success("a@x.com"), Success("b@x.com"), Success("c@x.com") },
            Channel = "fake",
            UsedNativeBatch = false
        };

        Assert.Equal(3, bulk.Total);
    }
}