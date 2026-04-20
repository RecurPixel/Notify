namespace RecurPixel.Notify.Tests;

public sealed class Msg91SmsChannelTests
{
    private static Msg91SmsOptions DefaultOptions => new()
    {
        AuthKey  = "test-authkey",
        SenderId = "SENDER",
        Route    = "4"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To   = "+919876543210",
        Body = "Hello from MSG91"
    };

    private static IHttpClientFactory MakeFactory(HttpStatusCode status, object responseBody)
    {
        var json    = JsonSerializer.Serialize(responseBody);
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
                Content    = new StringContent(json)
            });

        var client  = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    // ── success ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_Success_ReturnsTrueWithRequestId()
    {
        var channel = new Msg91SmsChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK, new { type = "success", message = "3702abc123" }),
            NullLogger<Msg91SmsChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("sms",        result.Channel);
        Assert.Equal("msg91",      result.Provider);
        Assert.Equal("3702abc123", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_ApiTypeError_ReturnsFalse()
    {
        var channel = new Msg91SmsChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK, new { type = "error", message = "Invalid auth key" }),
            NullLogger<Msg91SmsChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Invalid auth key", result.Error);
        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalse()
    {
        var channel = new Msg91SmsChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.Unauthorized, new { message = "Unauthorized" }),
            NullLogger<Msg91SmsChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("401", result.Error);
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

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
               .Returns(new HttpClient(handler.Object));

        var channel = new Msg91SmsChannel(
            Options.Create(DefaultOptions),
            factory.Object,
            NullLogger<Msg91SmsChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Connection refused", result.Error);
    }

    // ── bulk ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_Success_ReturnsAllSucceeded()
    {
        var payloads = new[]
        {
            new NotificationPayload { To = "+919876543210", Body = "Hi" },
            new NotificationPayload { To = "+919876543211", Body = "Hi" }
        };

        var channel = new Msg91SmsChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK, new { type = "success", message = "req-1" }),
            NullLogger<Msg91SmsChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.True(result.AllSucceeded);
        Assert.Equal(2,     result.Total);
        Assert.Equal("sms", result.Channel);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsSms()
    {
        var channel = new Msg91SmsChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK, new { }),
            NullLogger<Msg91SmsChannel>.Instance);

        Assert.Equal("sms", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var channel = new Msg91SmsChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK, new { type = "success", message = "req-1" }),
            NullLogger<Msg91SmsChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_AuthkeyHeader_IsSentInRequest()
    {
        HttpRequestMessage? captured = null;
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content    = new StringContent(JsonSerializer.Serialize(new { type = "success", message = "ok" }))
            });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
               .Returns(new HttpClient(handler.Object));

        var channel = new Msg91SmsChannel(
            Options.Create(DefaultOptions),
            factory.Object,
            NullLogger<Msg91SmsChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(captured);
        Assert.True(captured!.Headers.Contains("authkey"));
        Assert.Equal("test-authkey", captured.Headers.GetValues("authkey").First());
    }
}
