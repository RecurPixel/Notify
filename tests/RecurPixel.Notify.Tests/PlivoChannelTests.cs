using RecurPixel.Notify.Sms.Plivo;

namespace RecurPixel.Notify.Tests;

public sealed class PlivoChannelTests
{
    private static PlivoOptions DefaultOptions => new()
    {
        AuthId = "test-auth-id",
        AuthToken = "test-auth-token",
        FromNumber = "+15551234567"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To = "+447700900000",
        Body = "Hello from Plivo"
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
            message = "message(s) queued",
            message_uuid = new[] { "plivo-uuid-abc123" }
        };

        var channel = new PlivoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Accepted, response),
            NullLogger<PlivoChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("sms", result.Channel);
        Assert.Equal("plivo", result.Provider);
        Assert.Equal("plivo-uuid-abc123", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_SetsBasicAuthHeader()
    {
        var response = new
        {
            message = "message(s) queued",
            message_uuid = new[] { "uuid-1" }
        };

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
                StatusCode = HttpStatusCode.Accepted,
                Content = new StringContent(JsonSerializer.Serialize(response))
            });

        var channel = new PlivoChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<PlivoChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(capturedAuth);
        Assert.StartsWith("Basic ", capturedAuth);
    }

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalse()
    {
        var channel = new PlivoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Unauthorized, new { error = "Invalid credentials" }),
            NullLogger<PlivoChannel>.Instance);

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
            .ThrowsAsync(new HttpRequestException("DNS failure"));

        var channel = new PlivoChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<PlivoChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("DNS failure", result.Error);
    }

    [Fact]
    public async Task SendAsync_EmptyMessageUuid_ReturnsNullProviderId()
    {
        var response = new
        {
            message = "message(s) queued",
            message_uuid = Array.Empty<string>()
        };

        var channel = new PlivoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Accepted, response),
            NullLogger<PlivoChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Null(result.ProviderId);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsSms()
    {
        var channel = new PlivoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<PlivoChannel>.Instance);

        Assert.Equal("sms", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var response = new
        {
            message = "message(s) queued",
            message_uuid = new[] { "uuid-1" }
        };

        var channel = new PlivoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Accepted, response),
            NullLogger<PlivoChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_SetsChannelAndProvider()
    {
        var response = new
        {
            message = "message(s) queued",
            message_uuid = new[] { "uuid-1" }
        };

        var channel = new PlivoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Accepted, response),
            NullLogger<PlivoChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("sms", result.Channel);
        Assert.Equal("plivo", result.Provider);
    }
}
