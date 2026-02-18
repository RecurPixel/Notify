using dotAPNS;
using RecurPixel.Notify.Push.Apns;

namespace RecurPixel.Notify.Tests.Push;

public class ApnsChannelTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ApnsChannel MakeChannel(IApnsClient client) =>
        new(
            Options.Create(new ApnsOptions
            {
                KeyId = "KEYID12345",
                TeamId = "TEAMID1234",
                BundleId = "com.example.app",
                PrivateKey = "fake-p8-content"
            }),
            client,
            NullLogger<ApnsChannel>.Instance);

    private static NotificationPayload MakePayload(string token = "device-token-1") =>
        new() { To = token, Subject = "Test title", Body = "Test body" };

    private static Mock<IApnsClient> MakeSuccessMock()
    {
        var mock = new Mock<IApnsClient>();
        var response = ApnsResponse.Successful();
        mock.Setup(m => m.SendAsync(It.IsAny<ApplePush>()))
            .ReturnsAsync(response);
        return mock;
    }

    private static Mock<IApnsClient> MakeFailureMock(ApnsResponseReason reason)
    {
        var mock = new Mock<IApnsClient>();
        var response = ApnsResponse.Error(reason, reason.ToString());
        mock.Setup(m => m.SendAsync(It.IsAny<ApplePush>()))
            .ReturnsAsync(response);
        return mock;
    }

    private static Mock<IApnsClient> MakeThrowingMock(Exception? ex = null)
    {
        var mock = new Mock<IApnsClient>();
        mock.Setup(m => m.SendAsync(It.IsAny<ApplePush>()))
            .ThrowsAsync(ex ?? new InvalidOperationException("APNs connection failed"));
        return mock;
    }

    // ── ChannelName ───────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsPush()
    {
        var channel = MakeChannel(new Mock<IApnsClient>().Object);
        Assert.Equal("push", channel.ChannelName);
    }

    // ── SendAsync — success ───────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_Success_ReturnsSuccessResult()
    {
        var channel = MakeChannel(MakeSuccessMock().Object);

        var result = await channel.SendAsync(MakePayload());

        Assert.True(result.Success);
        Assert.Equal("push", result.Channel);
        Assert.Equal("apns", result.Provider);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_Success_RecipientMatchesPayloadTo()
    {
        var channel = MakeChannel(MakeSuccessMock().Object);

        var result = await channel.SendAsync(MakePayload("apns-token-abc"));

        Assert.Equal("apns-token-abc", result.Recipient);
    }

    [Fact]
    public async Task SendAsync_Success_SentAtIsRecentUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var channel = MakeChannel(MakeSuccessMock().Object);

        var result = await channel.SendAsync(MakePayload());

        Assert.True(result.SentAt >= before);
        Assert.True(result.SentAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task SendAsync_Success_ClientCalledOnce()
    {
        var mock = MakeSuccessMock();
        var channel = MakeChannel(mock.Object);

        await channel.SendAsync(MakePayload());

        mock.Verify(m => m.SendAsync(It.IsAny<ApplePush>()), Times.Once);
    }

    // ── SendAsync — APNs error response ──────────────────────────────────────

    [Fact]
    public async Task SendAsync_ApnsErrorResponse_ReturnsFailure()
    {
        var channel = MakeChannel(
            MakeFailureMock(ApnsResponseReason.BadDeviceToken).Object);

        var result = await channel.SendAsync(MakePayload());

        Assert.False(result.Success);
        Assert.Equal("push", result.Channel);
        Assert.Equal("apns", result.Provider);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task SendAsync_ApnsErrorResponse_RecipientStillSet()
    {
        var channel = MakeChannel(
            MakeFailureMock(ApnsResponseReason.Unregistered).Object);

        var result = await channel.SendAsync(MakePayload("expired-token"));

        Assert.Equal("expired-token", result.Recipient);
    }

    [Fact]
    public async Task SendAsync_ApnsErrorResponse_ErrorContainsReason()
    {
        var channel = MakeChannel(
            MakeFailureMock(ApnsResponseReason.BadDeviceToken).Object);

        var result = await channel.SendAsync(MakePayload());

        Assert.False(result.Success);
        Assert.NotEmpty(result.Error!);
    }

    // ── SendAsync — exception ─────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ClientThrows_ReturnsFailureResult()
    {
        var channel = MakeChannel(
            MakeThrowingMock(new InvalidOperationException("network timeout")).Object);

        var result = await channel.SendAsync(MakePayload());

        Assert.False(result.Success);
        Assert.Contains("network timeout", result.Error);
    }

    [Fact]
    public async Task SendAsync_ClientThrows_RecipientStillSet()
    {
        var channel = MakeChannel(MakeThrowingMock().Object);

        var result = await channel.SendAsync(MakePayload("throw-token"));

        Assert.Equal("throw-token", result.Recipient);
    }

    [Fact]
    public async Task SendAsync_ClientThrows_DoesNotPropagate()
    {
        var channel = MakeChannel(MakeThrowingMock().Object);

        var ex = await Record.ExceptionAsync(
            () => channel.SendAsync(MakePayload()));

        Assert.Null(ex);
    }

    // ── SendBulkAsync — base class loop ───────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_ThreePayloads_ClientCalledThreeTimes()
    {
        var mock = MakeSuccessMock();
        var channel = MakeChannel(mock.Object);

        var payloads = Enumerable.Range(1, 3)
            .Select(i => new NotificationPayload
            {
                To = $"token_{i}",
                Subject = "Title",
                Body = "Body"
            })
            .ToList();

        var result = await channel.SendBulkAsync(payloads);

        // Base class loop — one SendAsync call per payload
        mock.Verify(m => m.SendAsync(It.IsAny<ApplePush>()), Times.Exactly(3));
        Assert.Equal(3, result.Total);
        Assert.True(result.AllSucceeded);
    }

    [Fact]
    public async Task SendBulkAsync_UsedNativeBatch_IsFalse()
    {
        var channel = MakeChannel(MakeSuccessMock().Object);
        var payloads = new[]
        {
            new NotificationPayload { To = "t1", Subject = "s", Body = "b" }
        };

        var result = await channel.SendBulkAsync(payloads);

        // APNs has no native bulk — base class loop sets this to false
        Assert.False(result.UsedNativeBatch);
    }

    [Fact]
    public async Task SendBulkAsync_PartialFailure_ReflectedInResults()
    {
        var callCount = 0;
        var mock = new Mock<IApnsClient>();
        mock.Setup(m => m.SendAsync(It.IsAny<ApplePush>()))
            .ReturnsAsync(() =>
            {
                var n = Interlocked.Increment(ref callCount);
                return n % 2 == 0
                    ? ApnsResponse.Error(ApnsResponseReason.BadDeviceToken, "BadDeviceToken")
                    : ApnsResponse.Successful();
            });

        var channel = MakeChannel(mock.Object);
        var payloads = Enumerable.Range(1, 4)
            .Select(i => new NotificationPayload
            {
                To = $"token_{i}",
                Subject = "s",
                Body = "b"
            })
            .ToList();

        var result = await channel.SendBulkAsync(payloads);

        Assert.Equal(4, result.Total);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(2, result.FailureCount);
    }

    [Fact]
    public async Task SendBulkAsync_EmptyList_ReturnsEmptyResult()
    {
        var mock = MakeSuccessMock();
        var channel = MakeChannel(mock.Object);

        var result = await channel.SendBulkAsync(Array.Empty<NotificationPayload>());

        Assert.Equal(0, result.Total);
        mock.Verify(m => m.SendAsync(It.IsAny<ApplePush>()), Times.Never);
    }
}
