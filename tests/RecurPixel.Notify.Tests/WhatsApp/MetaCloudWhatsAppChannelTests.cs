using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.WhatsApp.MetaCloud;

namespace RecurPixel.Notify.Tests.WhatsApp;

public class MetaCloudWhatsAppChannelTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static MetaCloudWhatsAppChannel MakeChannel(IMetaCloudClient client) =>
        new(
            Options.Create(new MetaCloudOptions
            {
                AccessToken   = "test_token",
                PhoneNumberId = "1234567890"
            }),
            client,
            NullLogger<MetaCloudWhatsAppChannel>.Instance);

    private static NotificationPayload MakePayload(string to = "+9876543210") =>
        new() { To = to, Body = "Hello from WhatsApp" };

    private static Mock<IMetaCloudClient> MakeSuccessMock(string messageId = "wamid_abc") =>
        MakeClientMock(new MetaCloudSendResult { IsSuccess = true, MessageId = messageId });

    private static Mock<IMetaCloudClient> MakeFailureMock(string error = "rate limited") =>
        MakeClientMock(new MetaCloudSendResult { IsSuccess = false, Error = error });

    private static Mock<IMetaCloudClient> MakeClientMock(MetaCloudSendResult result)
    {
        var mock = new Mock<IMetaCloudClient>();
        mock.Setup(m => m.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return mock;
    }

    private static Mock<IMetaCloudClient> MakeThrowingMock(Exception? ex = null)
    {
        var mock = new Mock<IMetaCloudClient>();
        mock.Setup(m => m.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex ?? new HttpRequestException("connection refused"));
        return mock;
    }

    // ── ChannelName ───────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsWhatsapp()
    {
        Assert.Equal("whatsapp", MakeChannel(MakeSuccessMock().Object).ChannelName);
    }

    // ── SendAsync — success ───────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_Success_ReturnsSuccessResult()
    {
        var channel = MakeChannel(MakeSuccessMock("wamid_xyz").Object);
        var result  = await channel.SendAsync(MakePayload());

        Assert.True(result.Success);
        Assert.Equal("whatsapp",  result.Channel);
        Assert.Equal("metacloud", result.Provider);
        Assert.Equal("wamid_xyz", result.ProviderId);
    }

    [Fact]
    public async Task SendAsync_Success_RecipientMatchesPayloadTo()
    {
        var channel = MakeChannel(MakeSuccessMock().Object);
        var result  = await channel.SendAsync(MakePayload("+1112223333"));

        Assert.Equal("+1112223333", result.Recipient);
    }

    [Fact]
    public async Task SendAsync_Success_SentAtIsRecentUtc()
    {
        var before  = DateTime.UtcNow.AddSeconds(-1);
        var channel = MakeChannel(MakeSuccessMock().Object);
        var result  = await channel.SendAsync(MakePayload());

        Assert.True(result.SentAt >= before);
        Assert.True(result.SentAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task SendAsync_Success_PassesToAndBodyToClient()
    {
        var mock    = MakeSuccessMock();
        var channel = MakeChannel(mock.Object);

        await channel.SendAsync(new NotificationPayload
        {
            To   = "+9998887777",
            Body = "specific message"
        });

        mock.Verify(m => m.SendAsync(
            "+9998887777",
            "specific message",
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── SendAsync — API failure ───────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ApiFailure_ReturnsFailureResult()
    {
        var channel = MakeChannel(MakeFailureMock("rate limited").Object);
        var result  = await channel.SendAsync(MakePayload());

        Assert.False(result.Success);
        Assert.Equal("whatsapp",  result.Channel);
        Assert.Equal("metacloud", result.Provider);
        Assert.Contains("rate limited", result.Error);
    }

    [Fact]
    public async Task SendAsync_ApiFailure_RecipientStillSet()
    {
        var channel = MakeChannel(MakeFailureMock().Object);
        var result  = await channel.SendAsync(MakePayload("fail-recipient"));

        Assert.Equal("fail-recipient", result.Recipient);
    }

    // ── SendAsync — exception ─────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ClientThrows_ReturnsFailureResult()
    {
        var channel = MakeChannel(
            MakeThrowingMock(new HttpRequestException("network error")).Object);
        var result = await channel.SendAsync(MakePayload());

        Assert.False(result.Success);
        Assert.Contains("network error", result.Error);
    }

    [Fact]
    public async Task SendAsync_ClientThrows_DoesNotPropagate()
    {
        var channel = MakeChannel(MakeThrowingMock().Object);
        var ex      = await Record.ExceptionAsync(() => channel.SendAsync(MakePayload()));

        Assert.Null(ex);
    }

    [Fact]
    public async Task SendAsync_ClientThrows_RecipientStillSet()
    {
        var channel = MakeChannel(MakeThrowingMock().Object);
        var result  = await channel.SendAsync(MakePayload("throw-recipient"));

        Assert.Equal("throw-recipient", result.Recipient);
    }

    // ── SendBulkAsync — base class loop ───────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_ThreePayloads_ClientCalledThreeTimes()
    {
        var mock    = MakeSuccessMock();
        var channel = MakeChannel(mock.Object);

        var payloads = Enumerable.Range(1, 3)
            .Select(i => new NotificationPayload { To = $"+{i}111111111", Body = "msg" })
            .ToList();

        var result = await channel.SendBulkAsync(payloads);

        mock.Verify(m => m.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        Assert.Equal(3, result.Total);
        Assert.True(result.AllSucceeded);
    }

    [Fact]
    public async Task SendBulkAsync_UsedNativeBatch_IsFalse()
    {
        var channel  = MakeChannel(MakeSuccessMock().Object);
        var payloads = new[]
        {
            new NotificationPayload { To = "+1111111111", Body = "b" }
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.UsedNativeBatch);
    }

    [Fact]
    public async Task SendBulkAsync_EmptyList_ReturnsEmptyResult()
    {
        var mock    = MakeSuccessMock();
        var channel = MakeChannel(mock.Object);

        var result = await channel.SendBulkAsync(Array.Empty<NotificationPayload>());

        Assert.Equal(0, result.Total);
        mock.Verify(m => m.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
