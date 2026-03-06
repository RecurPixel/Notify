namespace RecurPixel.Notify.Tests;

public sealed class OneSignalChannelTests
{
    private static OneSignalOptions DefaultOptions => new()
    {
        AppId = "test-app-id",
        ApiKey = "test-rest-api-key"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To = "player-id-abc123",
        Subject = "Hello",
        Body = "World"
    };

    private static IHttpClientFactory MakeFactory(HttpStatusCode status, object responseBody)
    {
        var json = JsonSerializer.Serialize(responseBody);
        var handler = new Mock<HttpMessageHandler>();

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(json)
            });

        var client = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    // ── success ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_Success_ReturnsTrueWithNotificationId()
    {
        var response = new
        {
            id = "onesignal-notification-abc123",
            recipients = 1
        };

        var channel = new OneSignalChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK, response),
            NullLogger<OneSignalChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("push", result.Channel);
        Assert.Equal("onesignal", result.Provider);
        Assert.Equal("onesignal-notification-abc123", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_SetsBearerAuthHeader()
    {
        var handler = new Mock<HttpMessageHandler>();
        string? capturedAuth = null;

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capturedAuth = req.Headers.Authorization?.ToString();
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    id = "notif-1",
                    recipients = 1
                }))
            });

        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));
        var channel = new OneSignalChannel(
            Options.Create(DefaultOptions),
            clientFactory.Object,
            NullLogger<OneSignalChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(capturedAuth);
        Assert.Contains("Basic test-rest-api-key", capturedAuth);
    }

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalse()
    {
        var response = new { errors = new[] { "Invalid app_id" } };

        var channel = new OneSignalChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.BadRequest, response),
            NullLogger<OneSignalChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("400", result.Error);
        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_HttpException_ReturnsFalse()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));
        var channel = new OneSignalChannel(
            Options.Create(DefaultOptions),
            clientFactory.Object,
            NullLogger<OneSignalChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Connection refused", result.Error);
    }

    // ── bulk send ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_Success_ReturnsAllSucceeded()
    {
        var response = new
        {
            id = "bulk-notif-abc123",
            recipients = 2
        };

        var payloads = new[]
        {
            new NotificationPayload { To = "player-1", Subject = "Hi", Body = "Body" },
            new NotificationPayload { To = "player-2", Subject = "Hi", Body = "Body" }
        };

        var channel = new OneSignalChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK, response),
            NullLogger<OneSignalChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, result.Total);
        Assert.True(result.UsedNativeBatch);
        Assert.Equal("push", result.Channel);
    }

    [Fact]
    public async Task SendBulkAsync_ChunkFails_MarksAllInChunkFailed()
    {
        var payloads = new[]
        {
            new NotificationPayload { To = "player-1", Subject = "Hi", Body = "Body" },
            new NotificationPayload { To = "player-2", Subject = "Hi", Body = "Body" }
        };

        var channel = new OneSignalChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.BadRequest, new { errors = new[] { "Invalid app_id" } }),
            NullLogger<OneSignalChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.AllSucceeded);
        Assert.Equal(2, result.FailureCount);
    }

    [Fact]
    public async Task SendBulkAsync_SetsSharedNotificationIdAcrossRecipients()
    {
        var response = new
        {
            id = "shared-notif-id",
            recipients = 2
        };

        var payloads = new[]
        {
            new NotificationPayload { To = "player-1", Subject = "Hi", Body = "Body" },
            new NotificationPayload { To = "player-2", Subject = "Hi", Body = "Body" }
        };

        var channel = new OneSignalChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK, response),
            NullLogger<OneSignalChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.All(result.Results, r => Assert.Equal("shared-notif-id", r.ProviderId));
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsPush()
    {
        var channel = new OneSignalChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK, new { }),
            NullLogger<OneSignalChannel>.Instance);

        Assert.Equal("push", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var response = new { id = "notif-1", recipients = 1 };

        var channel = new OneSignalChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK, response),
            NullLogger<OneSignalChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_SetsChannelAndProvider()
    {
        var response = new { id = "notif-1", recipients = 1 };

        var channel = new OneSignalChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK, response),
            NullLogger<OneSignalChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("push", result.Channel);
        Assert.Equal("onesignal", result.Provider);
    }
}
