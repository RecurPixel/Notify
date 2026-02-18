using RecurPixel.Notify.Sms.Vonage;

namespace RecurPixel.Notify.Tests;

public sealed class VonageSmsChannelTests
{
    private static VonageOptions DefaultOptions => new()
    {
        ApiKey = "test-api-key",
        ApiSecret = "test-api-secret",
        FromNumber = "TestSender"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To = "+447700900000",
        Body = "Hello from Vonage"
    };

    // Vonage uses hyphenated keys (message-id, error-text) which C# anonymous
    // types cannot express — use Dictionary to produce correct JSON.
    private static object MakeVonageResponse(string status, string? messageId, string? errorText)
    {
        var msg = new Dictionary<string, object?>
        {
            ["status"] = status,
            ["message-id"] = messageId,
            ["error-text"] = errorText
        };
        return new { messages = new[] { msg } };
    }

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
        var response = MakeVonageResponse("0", "vonage-msg-abc123", null);

        var channel = new VonageSmsChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<VonageSmsChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("sms", result.Channel);
        Assert.Equal("vonage", result.Provider);
        Assert.Equal("vonage-msg-abc123", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_VonageStatusNonZero_ReturnsFalse()
    {
        var response = MakeVonageResponse("4", null, "Invalid credentials");

        var channel = new VonageSmsChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<VonageSmsChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("4", result.Error);
        Assert.Contains("Invalid credentials", result.Error);
        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalse()
    {
        var channel = new VonageSmsChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Unauthorized, new { message = "Unauthorized" }),
            NullLogger<VonageSmsChannel>.Instance);

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

        var channel = new VonageSmsChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<VonageSmsChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Connection refused", result.Error);
    }

    // ── bulk send ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_Success_ReturnsAllSucceeded()
    {
        var response = MakeVonageResponse("0", "msg-1", null);

        var payloads = new[]
        {
            new NotificationPayload { To = "+447700900001", Body = "Hi" },
            new NotificationPayload { To = "+447700900002", Body = "Hi" }
        };

        var channel = new VonageSmsChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<VonageSmsChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, result.Total);
        Assert.Equal("sms", result.Channel);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsSms()
    {
        var channel = new VonageSmsChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<VonageSmsChannel>.Instance);

        Assert.Equal("sms", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var response = MakeVonageResponse("0", "msg-1", null);

        var channel = new VonageSmsChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<VonageSmsChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }
}
