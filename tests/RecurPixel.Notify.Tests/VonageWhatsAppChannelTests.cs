using RecurPixel.Notify.WhatsApp.Vonage;

namespace RecurPixel.Notify.Tests;

public sealed class VonageWhatsAppChannelTests
{
    private static VonageWhatsAppOptions DefaultOptions => new()
    {
        ApiKey = "test-api-key",
        ApiSecret = "test-api-secret",
        FromNumber = "447700900001"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To = "447700900000",
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
    public async Task SendAsync_Success_ReturnsTrueWithMessageUuid()
    {
        var response = new { message_uuid = "vonage-wa-uuid-abc123" };

        var channel = new VonageWhatsAppChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Accepted, response),
            NullLogger<VonageWhatsAppChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("whatsapp", result.Channel);
        Assert.Equal("vonage", result.Provider);
        Assert.Equal("vonage-wa-uuid-abc123", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_SetsBasicAuthHeader()
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
                StatusCode = HttpStatusCode.Accepted,
                Content = new StringContent(JsonSerializer.Serialize(
                    new { message_uuid = "uuid-1" }))
            });

        var channel = new VonageWhatsAppChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<VonageWhatsAppChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(capturedAuth);
        Assert.StartsWith("Basic ", capturedAuth);
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
                StatusCode = HttpStatusCode.Accepted,
                Content = new StringContent(JsonSerializer.Serialize(
                    new { message_uuid = "uuid-1" }))
            });

        var channel = new VonageWhatsAppChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<VonageWhatsAppChannel>.Instance);

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
            To = "447700900000",
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
                StatusCode = HttpStatusCode.Accepted,
                Content = new StringContent(JsonSerializer.Serialize(
                    new { message_uuid = "uuid-1" }))
            });

        var channel = new VonageWhatsAppChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<VonageWhatsAppChannel>.Instance);

        await channel.SendAsync(payload);

        Assert.NotNull(capturedBody);
        Assert.Contains("Body only", capturedBody);
        Assert.DoesNotContain("\n\n", capturedBody);
    }

    // ── failure ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalse()
    {
        var response = new { title = "Unauthorized", detail = "Invalid credentials" };

        var channel = new VonageWhatsAppChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Unauthorized, response),
            NullLogger<VonageWhatsAppChannel>.Instance);

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

        var channel = new VonageWhatsAppChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<VonageWhatsAppChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("DNS failure", result.Error);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsWhatsApp()
    {
        var channel = new VonageWhatsAppChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<VonageWhatsAppChannel>.Instance);

        Assert.Equal("whatsapp", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var response = new { message_uuid = "uuid-1" };

        var channel = new VonageWhatsAppChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Accepted, response),
            NullLogger<VonageWhatsAppChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_SetsChannelAndProvider()
    {
        var response = new { message_uuid = "uuid-1" };

        var channel = new VonageWhatsAppChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Accepted, response),
            NullLogger<VonageWhatsAppChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("whatsapp", result.Channel);
        Assert.Equal("vonage", result.Provider);
    }
}
