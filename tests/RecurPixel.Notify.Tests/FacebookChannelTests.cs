using RecurPixel.Notify.Facebook;

namespace RecurPixel.Notify.Tests;

public sealed class FacebookChannelTests
{
    private static FacebookOptions DefaultOptions => new()
    {
        PageAccessToken = "test-page-access-token"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To = "psid-123456789",
        Subject = "Hello",
        Body = "World"
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

    // ── success ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_Success_ReturnsTrueWithMessageId()
    {
        var response = new
        {
            message_id = "mid.abc123",
            recipient_id = "psid-123456789"
        };

        var channel = new FacebookChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<FacebookChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("facebook", result.Channel);
        Assert.Equal("facebook", result.Provider);
        Assert.Equal("mid.abc123", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_NoSubject_SendsBodyOnly()
    {
        var payload = new NotificationPayload
        {
            To = "psid-123456789",
            Subject = "",
            Body = "Body only"
        };

        var response = new
        {
            message_id = "mid.xyz",
            recipient_id = "psid-123456789"
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
                Content = new StringContent(JsonSerializer.Serialize(response))
            });

        var channel = new FacebookChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<FacebookChannel>.Instance);

        var result = await channel.SendAsync(payload);

        Assert.True(result.Success);
        Assert.NotNull(capturedBody);
        Assert.Contains("Body only", capturedBody);
        Assert.DoesNotContain("\n\n", capturedBody);
    }

    [Fact]
    public async Task SendAsync_WithSubject_CombinesSubjectAndBody()
    {
        var handler = new Mock<HttpMessageHandler>();
        string? capturedBody = null;

        var response = new
        {
            message_id = "mid.xyz",
            recipient_id = "psid-123456789"
        };

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
                Content = new StringContent(JsonSerializer.Serialize(response))
            });

        var channel = new FacebookChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<FacebookChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.NotNull(capturedBody);
        Assert.Contains("Hello", capturedBody);
        Assert.Contains("World", capturedBody);
    }

    // ── failure ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalseWithError()
    {
        var response = new
        {
            error = new { message = "Invalid OAuth access token" }
        };

        var channel = new FacebookChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Unauthorized, response),
            NullLogger<FacebookChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Equal("facebook", result.Channel);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Contains("401", result.Error);
    }

    [Fact]
    public async Task SendAsync_HttpException_ReturnsFalseWithExceptionMessage()
    {
        var handler = new Mock<HttpMessageHandler>();

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var channel = new FacebookChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<FacebookChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Connection refused", result.Error);
        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsFacebook()
    {
        var channel = new FacebookChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<FacebookChannel>.Instance);

        Assert.Equal("facebook", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var response = new
        {
            message_id = "mid.abc",
            recipient_id = "psid-123456789"
        };

        var channel = new FacebookChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<FacebookChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_SetsChannelAndProvider()
    {
        var response = new
        {
            message_id = "mid.abc",
            recipient_id = "psid-123456789"
        };

        var channel = new FacebookChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<FacebookChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("facebook", result.Channel);
        Assert.Equal("facebook", result.Provider);
    }
}
