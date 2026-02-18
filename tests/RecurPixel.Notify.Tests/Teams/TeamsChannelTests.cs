using RecurPixel.Notify.Teams;

namespace RecurPixel.Notify.Tests.Teams;

public class TeamsChannelTests
{
    private static TeamsChannel BuildChannel(HttpStatusCode statusCode, string responseBody = "1")
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

        var options = Options.Create(new TeamsOptions
        {
            WebhookUrl = "https://outlook.office.com/webhook/test"
        });

        return new TeamsChannel(options, factoryMock.Object, NullLogger<TeamsChannel>.Instance);
    }

    [Fact]
    public async Task SendAsync_SuccessResponse_ReturnsSuccess()
    {
        var channel = BuildChannel(HttpStatusCode.OK);
        var payload = new NotificationPayload
        {
            To = "engineering-channel",
            Subject = "Build passed",
            Body = "Pipeline completed successfully."
        };

        var result = await channel.SendAsync(payload);

        Assert.True(result.Success);
        Assert.Equal("teams", result.Channel);
        Assert.Equal("teams", result.Provider);
        Assert.Equal("engineering-channel", result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_NoSubject_ReturnsSuccess()
    {
        var channel = BuildChannel(HttpStatusCode.OK);
        var payload = new NotificationPayload
        {
            To = "engineering-channel",
            Body = "Body only — no subject provided"
        };

        var result = await channel.SendAsync(payload);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task SendAsync_WebhookReturnsError_ReturnsFailure()
    {
        var channel = BuildChannel(HttpStatusCode.BadRequest, "Microsoft Teams endpoint error");
        var payload = new NotificationPayload
        {
            To = "engineering-channel",
            Body = "Test"
        };

        var result = await channel.SendAsync(payload);

        Assert.False(result.Success);
        Assert.Equal("teams", result.Channel);
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
            .ThrowsAsync(new HttpRequestException("timeout"));

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var options = Options.Create(new TeamsOptions
        {
            WebhookUrl = "https://outlook.office.com/webhook/test"
        });

        var channel = new TeamsChannel(options, factoryMock.Object, NullLogger<TeamsChannel>.Instance);
        var payload = new NotificationPayload { To = "engineering-channel", Body = "Test" };

        var result = await channel.SendAsync(payload);

        Assert.False(result.Success);
        Assert.Contains("timeout", result.Error);
    }

    [Fact]
    public void ChannelName_ReturnsTeams()
    {
        var channel = BuildChannel(HttpStatusCode.OK);
        Assert.Equal("teams", channel.ChannelName);
    }
}
