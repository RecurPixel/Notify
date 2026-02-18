using RecurPixel.Notify.Slack;

namespace RecurPixel.Notify.Tests.Slack;

public class SlackChannelTests
{
    private static SlackChannel BuildChannel(HttpStatusCode statusCode, string responseBody = "ok")
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

        var options = Options.Create(new SlackOptions
        {
            WebhookUrl = "https://hooks.slack.com/services/test"
        });

        return new SlackChannel(options, factoryMock.Object, NullLogger<SlackChannel>.Instance);
    }

    [Fact]
    public async Task SendAsync_SuccessResponse_ReturnsSuccess()
    {
        var channel = BuildChannel(HttpStatusCode.OK);
        var payload = new NotificationPayload
        {
            To = "#general",
            Subject = "Hello",
            Body = "World"
        };

        var result = await channel.SendAsync(payload);

        Assert.True(result.Success);
        Assert.Equal("slack", result.Channel);
        Assert.Equal("slack", result.Provider);
        Assert.Equal("#general", result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_NoSubject_ReturnsSuccess()
    {
        var channel = BuildChannel(HttpStatusCode.OK);
        var payload = new NotificationPayload
        {
            To = "#alerts",
            Body = "Simple message with no subject"
        };

        var result = await channel.SendAsync(payload);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task SendAsync_WebhookReturnsError_ReturnsFailure()
    {
        var channel = BuildChannel(HttpStatusCode.BadRequest, "invalid_payload");
        var payload = new NotificationPayload
        {
            To = "#general",
            Body = "Test"
        };

        var result = await channel.SendAsync(payload);

        Assert.False(result.Success);
        Assert.Equal("slack", result.Channel);
        Assert.Contains("400", result.Error);
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
            .ThrowsAsync(new HttpRequestException("network error"));

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var options = Options.Create(new SlackOptions
        {
            WebhookUrl = "https://hooks.slack.com/services/test"
        });

        var channel = new SlackChannel(options, factoryMock.Object, NullLogger<SlackChannel>.Instance);
        var payload = new NotificationPayload { To = "#general", Body = "Test" };

        var result = await channel.SendAsync(payload);

        Assert.False(result.Success);
        Assert.Contains("network error", result.Error);
    }

    [Fact]
    public void ChannelName_ReturnsSlack()
    {
        var channel = BuildChannel(HttpStatusCode.OK);
        Assert.Equal("slack", channel.ChannelName);
    }
}
