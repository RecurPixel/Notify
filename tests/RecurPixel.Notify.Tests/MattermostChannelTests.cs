namespace RecurPixel.Notify.Tests;

public sealed class MattermostChannelTests
{
    private static MattermostOptions DefaultOptions => new()
    {
        WebhookUrl = "https://mattermost.example.com/hooks/test-hook-id",
        Username = "TestBot",
        Channel = "town-square"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To = "channel",
        Subject = "Hello",
        Body = "World"
    };

    private static IHttpClientFactory MakeFactory(HttpStatusCode status, string responseBody = "ok")
    {
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
                Content = new StringContent(responseBody)
            });

        var client = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    // ── success ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_Success_ReturnsTrue()
    {
        var channel = new MattermostChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK),
            NullLogger<MattermostChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("mattermost", result.Channel);
        Assert.Equal("mattermost", result.Provider);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_WithSubject_CombinesSubjectAndBody()
    {
        var handler = new Mock<HttpMessageHandler>();
        string? capturedBody = null;

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("ok")
            });

        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));
        var channel = new MattermostChannel(
            Options.Create(DefaultOptions),
            clientFactory.Object,
            NullLogger<MattermostChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(capturedBody);
        Assert.Contains("Hello", capturedBody);
        Assert.Contains("World", capturedBody);
    }

    [Fact]
    public async Task SendAsync_NoSubject_SendsBodyOnly()
    {
        var payload = new NotificationPayload
        {
            To = "channel",
            Subject = "",
            Body = "Body only"
        };

        var handler = new Mock<HttpMessageHandler>();
        string? capturedBody = null;

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("ok")
            });

        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));
        var channel = new MattermostChannel(
            Options.Create(DefaultOptions),
            clientFactory.Object,
            NullLogger<MattermostChannel>.Instance);

        await channel.SendAsync(payload);

        Assert.NotNull(capturedBody);
        Assert.Contains("Body only", capturedBody);
        Assert.DoesNotContain("**", capturedBody);
    }

    [Fact]
    public async Task SendAsync_SendsToConfiguredWebhookUrl()
    {
        var handler = new Mock<HttpMessageHandler>();
        Uri? capturedUri = null;

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capturedUri = req.RequestUri;
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("ok")
            });

        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));
        var channel = new MattermostChannel(
            Options.Create(DefaultOptions),
            clientFactory.Object,
            NullLogger<MattermostChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(capturedUri);
        Assert.Equal(DefaultOptions.WebhookUrl, capturedUri!.ToString());
    }

    // ── failure ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalse()
    {
        var channel = new MattermostChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.Unauthorized, "Invalid token"),
            NullLogger<MattermostChannel>.Instance);

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

        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));
        var channel = new MattermostChannel(
            Options.Create(DefaultOptions),
            clientFactory.Object,
            NullLogger<MattermostChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Connection refused", result.Error);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsMattermost()
    {
        var channel = new MattermostChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK),
            NullLogger<MattermostChannel>.Instance);

        Assert.Equal("mattermost", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var channel = new MattermostChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK),
            NullLogger<MattermostChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_SetsChannelAndProvider()
    {
        var channel = new MattermostChannel(
            Options.Create(DefaultOptions),
            MakeFactory(HttpStatusCode.OK),
            NullLogger<MattermostChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("mattermost", result.Channel);
        Assert.Equal("mattermost", result.Provider);
    }
}
