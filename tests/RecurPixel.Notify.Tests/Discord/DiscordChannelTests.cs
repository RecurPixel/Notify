using RecurPixel.Notify.Discord;

namespace RecurPixel.Notify.Tests.Discord;

public class DiscordChannelTests
{
    private static DiscordChannel BuildChannel(HttpStatusCode statusCode, string responseBody = "")
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseBody)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var options = Options.Create(new DiscordOptions
        {
            WebhookUrl = "https://discord.com/api/webhooks/test"
        });

        return new DiscordChannel(options, factoryMock.Object, NullLogger<DiscordChannel>.Instance);
    }

    [Fact]
    public async Task SendAsync_SuccessResponse_ReturnsSuccess()
    {
        var channel = BuildChannel(HttpStatusCode.NoContent);
        var payload = new NotificationPayload
        {
            To = "server-alerts",
            Subject = "Deployment complete",
            Body = "v2.1.0 is live."
        };

        var result = await channel.SendAsync(payload);

        Assert.True(result.Success);
        Assert.Equal("discord", result.Channel);
        Assert.Equal("discord", result.Provider);
        Assert.Equal("server-alerts", result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_NoSubject_ReturnsSuccess()
    {
        var channel = BuildChannel(HttpStatusCode.NoContent);
        var payload = new NotificationPayload
        {
            To = "server-alerts",
            Body = "Plain body with no subject"
        };

        var result = await channel.SendAsync(payload);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task SendAsync_WebhookReturnsError_ReturnsFailure()
    {
        var channel = BuildChannel(HttpStatusCode.Unauthorized, "401: Unauthorized");
        var payload = new NotificationPayload
        {
            To = "server-alerts",
            Body = "Test"
        };

        var result = await channel.SendAsync(payload);

        Assert.False(result.Success);
        Assert.Equal("discord", result.Channel);
        Assert.Contains("401", result.Error);
    }

    [Fact]
    public async Task SendAsync_HttpClientThrows_ReturnsFailure()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var options = Options.Create(new DiscordOptions
        {
            WebhookUrl = "https://discord.com/api/webhooks/test"
        });

        var channel = new DiscordChannel(options, factoryMock.Object, NullLogger<DiscordChannel>.Instance);
        var payload = new NotificationPayload { To = "server-alerts", Body = "Test" };

        var result = await channel.SendAsync(payload);

        Assert.False(result.Success);
        Assert.Contains("connection refused", result.Error);
    }

    [Fact]
    public void ChannelName_ReturnsDiscord()
    {
        var channel = BuildChannel(HttpStatusCode.NoContent);
        Assert.Equal("discord", channel.ChannelName);
    }
}
