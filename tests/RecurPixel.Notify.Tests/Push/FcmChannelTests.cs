using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Push.Fcm;

namespace RecurPixel.Notify.Tests.Push;

public class FcmChannelTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FcmChannel MakeChannel(IFcmMessagingClient client) =>
        new(
            Options.Create(new FcmOptions
            {
                ProjectId = "test-project",
                ServiceAccountJson = "{}"
            }),
            client,
            NullLogger<FcmChannel>.Instance);

    private static NotificationPayload MakePayload(string token = "device-token-1") =>
        new() { To = token, Subject = "Test title", Body = "Test body" };

    private static Mock<IFcmMessagingClient> MakeClientMock(
        string messageId = "msg_123",
        bool throwOnSend = false,
        Exception? throws = null)
    {
        var mock = new Mock<IFcmMessagingClient>();

        if (throwOnSend)
        {
            mock.Setup(m => m.SendAsync(
                    It.IsAny<string>(), It.IsAny<string?>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(throws ?? new InvalidOperationException("FCM error"));
        }
        else
        {
            mock.Setup(m => m.SendAsync(
                    It.IsAny<string>(), It.IsAny<string?>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(messageId);
        }

        return mock;
    }

    // ── ChannelName ───────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsPush()
    {
        var channel = MakeChannel(new Mock<IFcmMessagingClient>().Object);
        Assert.Equal("push", channel.ChannelName);
    }

    // ── SendAsync — success ───────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_Success_ReturnsSuccessResult()
    {
        var client = MakeClientMock(messageId: "fcm_abc");
        var channel = MakeChannel(client.Object);

        var result = await channel.SendAsync(MakePayload());

        Assert.True(result.Success);
        Assert.Equal("push", result.Channel);
        Assert.Equal("fcm", result.Provider);
        Assert.Equal("fcm_abc", result.ProviderId);
    }

    [Fact]
    public async Task SendAsync_Success_RecipientMatchesPayloadTo()
    {
        var client = MakeClientMock();
        var channel = MakeChannel(client.Object);

        var result = await channel.SendAsync(MakePayload("token-xyz"));

        Assert.Equal("token-xyz", result.Recipient);
    }

    [Fact]
    public async Task SendAsync_Success_SentAtIsRecentUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var client = MakeClientMock();
        var channel = MakeChannel(client.Object);

        var result = await channel.SendAsync(MakePayload());

        Assert.True(result.SentAt >= before);
        Assert.True(result.SentAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task SendAsync_Success_PassesTokenTitleBodyToClient()
    {
        var mock = new Mock<IFcmMessagingClient>();
        mock.Setup(m => m.SendAsync(
                It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("msg_1");

        var channel = MakeChannel(mock.Object);
        var payload = new NotificationPayload
        {
            To = "device-abc",
            Subject = "My Title",
            Body = "My Body"
        };

        await channel.SendAsync(payload);

        mock.Verify(m => m.SendAsync(
            "device-abc", "My Title", "My Body", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── SendAsync — failure ───────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ClientThrows_ReturnsFailureResult()
    {
        var client = MakeClientMock(throwOnSend: true,
            throws: new InvalidOperationException("registration not found"));
        var channel = MakeChannel(client.Object);

        var result = await channel.SendAsync(MakePayload());

        Assert.False(result.Success);
        Assert.Equal("push", result.Channel);
        Assert.Equal("fcm", result.Provider);
        Assert.Contains("registration not found", result.Error);
    }

    [Fact]
    public async Task SendAsync_ClientThrows_RecipientStillSet()
    {
        var client = MakeClientMock(throwOnSend: true);
        var channel = MakeChannel(client.Object);

        var result = await channel.SendAsync(MakePayload("bad-token"));

        Assert.Equal("bad-token", result.Recipient);
    }

    [Fact]
    public async Task SendAsync_ClientThrows_DoesNotThrow()
    {
        var client = MakeClientMock(throwOnSend: true);
        var channel = MakeChannel(client.Object);

        // Must not propagate — exceptions are swallowed into NotifyResult
        var ex = await Record.ExceptionAsync(() => channel.SendAsync(MakePayload()));
        Assert.Null(ex);
    }

    // ── SendBulkAsync — success ───────────────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_AllSucceed_UsedNativeBatchTrue()
    {
        var mock = new Mock<IFcmMessagingClient>();
        mock.Setup(m => m.SendMulticastAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> tokens, string? _, string _, CancellationToken _) =>
                tokens.Select((_, i) => new FcmSendResponse
                {
                    IsSuccess = true,
                    MessageId = $"msg_{i}"
                }).ToList());

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

        Assert.True(result.UsedNativeBatch);
        Assert.True(result.AllSucceeded);
        Assert.Equal(3, result.Total);
    }

    [Fact]
    public async Task SendBulkAsync_ResultsInSameOrderAsInput()
    {
        var mock = new Mock<IFcmMessagingClient>();
        mock.Setup(m => m.SendMulticastAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> tokens, string? _, string _, CancellationToken _) =>
                tokens.Select((t, i) => new FcmSendResponse
                {
                    IsSuccess = true,
                    MessageId = $"id_for_{t}"
                }).ToList());

        var channel = MakeChannel(mock.Object);
        var payloads = new[]
        {
            new NotificationPayload { To = "alpha", Subject = "s", Body = "b" },
            new NotificationPayload { To = "beta",  Subject = "s", Body = "b" },
            new NotificationPayload { To = "gamma", Subject = "s", Body = "b" }
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.Equal("alpha", result.Results[0].Recipient);
        Assert.Equal("beta", result.Results[1].Recipient);
        Assert.Equal("gamma", result.Results[2].Recipient);
    }

    [Fact]
    public async Task SendBulkAsync_PartialFailure_ReflectedInResults()
    {
        var mock = new Mock<IFcmMessagingClient>();
        mock.Setup(m => m.SendMulticastAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FcmSendResponse>
            {
                new() { IsSuccess = true,  MessageId = "msg_ok" },
                new() { IsSuccess = false, Error     = "invalid token" },
                new() { IsSuccess = true,  MessageId = "msg_ok2" }
            });

        var channel = MakeChannel(mock.Object);
        var payloads = Enumerable.Range(1, 3)
            .Select(i => new NotificationPayload { To = $"t{i}", Subject = "s", Body = "b" })
            .ToList();

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.AllSucceeded);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Equal("invalid token", result.Failures[0].Error);
        Assert.Equal("t2", result.Failures[0].Recipient);
    }

    // ── SendBulkAsync — chunking ──────────────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_MoreThan500Payloads_SendMulticastCalledTwice()
    {
        var mock = new Mock<IFcmMessagingClient>();
        mock.Setup(m => m.SendMulticastAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> tokens, string? _, string _, CancellationToken _) =>
                tokens.Select(_ => new FcmSendResponse { IsSuccess = true, MessageId = "m" }).ToList());

        var channel = MakeChannel(mock.Object);
        var payloads = Enumerable.Range(1, 501)
            .Select(i => new NotificationPayload { To = $"tok_{i}", Subject = "s", Body = "b" })
            .ToList();

        var result = await channel.SendBulkAsync(payloads);

        // 501 payloads → chunk of 500 + chunk of 1 → two multicast calls
        mock.Verify(
            m => m.SendMulticastAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        Assert.Equal(501, result.Total);
        Assert.True(result.AllSucceeded);
    }

    [Fact]
    public async Task SendBulkAsync_ExactlyAt500_SendMulticastCalledOnce()
    {
        var mock = new Mock<IFcmMessagingClient>();
        mock.Setup(m => m.SendMulticastAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> tokens, string? _, string _, CancellationToken _) =>
                tokens.Select(_ => new FcmSendResponse { IsSuccess = true, MessageId = "m" }).ToList());

        var channel = MakeChannel(mock.Object);
        var payloads = Enumerable.Range(1, 500)
            .Select(i => new NotificationPayload { To = $"tok_{i}", Subject = "s", Body = "b" })
            .ToList();

        await channel.SendBulkAsync(payloads);

        mock.Verify(
            m => m.SendMulticastAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── SendBulkAsync — chunk exception handling ──────────────────────────────

    [Fact]
    public async Task SendBulkAsync_ChunkThrows_AllInChunkReturnFailure()
    {
        var mock = new Mock<IFcmMessagingClient>();
        mock.Setup(m => m.SendMulticastAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("FCM unavailable"));

        var channel = MakeChannel(mock.Object);
        var payloads = Enumerable.Range(1, 3)
            .Select(i => new NotificationPayload { To = $"t{i}", Subject = "s", Body = "b" })
            .ToList();

        var result = await channel.SendBulkAsync(payloads);

        Assert.Equal(3, result.Total);
        Assert.Equal(0, result.SuccessCount);
        Assert.All(result.Results, r =>
        {
            Assert.False(r.Success);
            Assert.Contains("FCM unavailable", r.Error);
        });
    }

    [Fact]
    public async Task SendBulkAsync_ChunkThrows_DoesNotPropagate()
    {
        var mock = new Mock<IFcmMessagingClient>();
        mock.Setup(m => m.SendMulticastAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var channel = MakeChannel(mock.Object);
        var payloads = new[]
        {
            new NotificationPayload { To = "t1", Subject = "s", Body = "b" }
        };

        var ex = await Record.ExceptionAsync(() => channel.SendBulkAsync(payloads));
        Assert.Null(ex);
    }

    // ── SendBulkAsync — empty list ────────────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_EmptyList_ReturnsEmptyResult()
    {
        var mock = new Mock<IFcmMessagingClient>();
        var channel = MakeChannel(mock.Object);

        var result = await channel.SendBulkAsync(Array.Empty<NotificationPayload>());

        Assert.Equal(0, result.Total);
        Assert.True(result.AllSucceeded);
        Assert.True(result.UsedNativeBatch);

        mock.Verify(
            m => m.SendMulticastAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
