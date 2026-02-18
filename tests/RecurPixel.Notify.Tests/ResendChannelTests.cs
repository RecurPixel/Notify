using RecurPixel.Notify.Email.Resend;

namespace RecurPixel.Notify.Tests;

public sealed class ResendChannelTests
{
    private static ResendOptions DefaultOptions => new()
    {
        ApiKey = "re_test_key",
        FromEmail = "no-reply@example.com",
        FromName = "Test"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To = "recipient@example.com",
        Subject = "Hello",
        Body = "Plain text body"
    };

    private static HttpClient MakeClient(HttpStatusCode status, object responseBody)
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

        return new HttpClient(handler.Object);
    }

    [Fact]
    public async Task SendAsync_Success_ReturnsTrueWithMessageId()
    {
        var response = new { id = "resend-msg-abc123" };

        var channel = new ResendChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<ResendChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("email", result.Channel);
        Assert.Equal("resend", result.Provider);
        Assert.Equal("resend-msg-abc123", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_HtmlBody_SendsAsHtml()
    {
        var payload = new NotificationPayload
        {
            To = "recipient@example.com",
            Subject = "Hello",
            Body = "<h1>Hello World</h1>"
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
                Content = new StringContent(JsonSerializer.Serialize(new { id = "abc" }))
            });

        var channel = new ResendChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<ResendChannel>.Instance);

        await channel.SendAsync(payload);

        Assert.NotNull(capturedBody);
        Assert.Contains("html", capturedBody);
        Assert.DoesNotContain("\"text\"", capturedBody);
    }

    [Fact]
    public async Task SendAsync_PlainBody_SendsAsText()
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
                Content = new StringContent(JsonSerializer.Serialize(new { id = "abc" }))
            });

        var channel = new ResendChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<ResendChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(capturedBody);
        Assert.Contains("\"text\"", capturedBody);
        Assert.DoesNotContain("\"html\"", capturedBody);
    }

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalse()
    {
        var channel = new ResendChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.UnprocessableEntity, new { message = "Invalid email" }),
            NullLogger<ResendChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("422", result.Error);
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

        var channel = new ResendChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<ResendChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Connection refused", result.Error);
    }

    [Fact]
    public async Task SendAsync_SetsAuthorizationHeader()
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
                Content = new StringContent(JsonSerializer.Serialize(new { id = "abc" }))
            });

        var channel = new ResendChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<ResendChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(capturedAuth);
        Assert.Contains("Bearer re_test_key", capturedAuth);
    }

    [Fact]
    public void ChannelName_IsEmail()
    {
        var channel = new ResendChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<ResendChannel>.Instance);

        Assert.Equal("email", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var response = new { id = "abc" };

        var channel = new ResendChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<ResendChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }
}
