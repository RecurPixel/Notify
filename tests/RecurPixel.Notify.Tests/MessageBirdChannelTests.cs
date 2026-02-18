using RecurPixel.Notify.Sms.MessageBird;

namespace RecurPixel.Notify.Tests;

public sealed class MessageBirdChannelTests
{
    private static MessageBirdOptions DefaultOptions => new()
    {
        ApiKey = "test-access-key",
        Originator = "TestSender"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To = "+447700900000",
        Body = "Hello from MessageBird"
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
            id = "messagebird-msg-abc123",
            reference = (string?)null
        };

        var channel = new MessageBirdChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Created, response),
            NullLogger<MessageBirdChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("sms", result.Channel);
        Assert.Equal("messagebird", result.Provider);
        Assert.Equal("messagebird-msg-abc123", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_SetsAccessKeyAuthHeader()
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
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    id = "msg-1",
                    reference = (string?)null
                }))
            });

        var channel = new MessageBirdChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<MessageBirdChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(capturedAuth);
        Assert.Contains("AccessKey test-access-key", capturedAuth);
    }

    [Fact]
    public async Task SendAsync_SendsRecipientsAsArray()
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
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    id = "msg-1",
                    reference = (string?)null
                }))
            });

        var channel = new MessageBirdChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<MessageBirdChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(capturedBody);
        Assert.Contains("recipients", capturedBody);
        // Check for the number without '+' — System.Text.Json escapes '+' to \u002B
        // by default; the digits alone are sufficient to verify the recipient was set
        Assert.Contains("447700900000", capturedBody);
    }

    // ── failure ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalse()
    {
        var response = new
        {
            errors = new[]
            {
                new { code = 2, description = "Request not allowed", parameter = (string?)null }
            }
        };

        var channel = new MessageBirdChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Unauthorized, response),
            NullLogger<MessageBirdChannel>.Instance);

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
            .ThrowsAsync(new HttpRequestException("Network error"));

        var channel = new MessageBirdChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<MessageBirdChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Network error", result.Error);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsSms()
    {
        var channel = new MessageBirdChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<MessageBirdChannel>.Instance);

        Assert.Equal("sms", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var response = new { id = "msg-1", reference = (string?)null };

        var channel = new MessageBirdChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Created, response),
            NullLogger<MessageBirdChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_SetsChannelAndProvider()
    {
        var response = new { id = "msg-1", reference = (string?)null };

        var channel = new MessageBirdChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Created, response),
            NullLogger<MessageBirdChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("sms", result.Channel);
        Assert.Equal("messagebird", result.Provider);
    }
}
