using RecurPixel.Notify.Sms.Sinch;

namespace RecurPixel.Notify.Tests;

public sealed class SinchChannelTests
{
    private static SinchOptions DefaultOptions => new()
    {
        ApiToken = "test-api-token",
        ServicePlanId = "test-service-plan-id",
        FromNumber = "+15551234567"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To = "+447700900000",
        Body = "Hello from Sinch"
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
            id = "sinch-batch-abc123",
            type = "mt_text"
        };

        var channel = new SinchChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Created, response),
            NullLogger<SinchChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("sms", result.Channel);
        Assert.Equal("sinch", result.Provider);
        Assert.Equal("sinch-batch-abc123", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_SetsBearerAuthHeader()
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
                    id = "batch-1",
                    type = "mt_text"
                }))
            });

        var channel = new SinchChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<SinchChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(capturedAuth);
        Assert.Contains("Bearer test-api-token", capturedAuth);
    }

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalse()
    {
        var channel = new SinchChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Unauthorized, new { code = 401, text = "Unauthorized" }),
            NullLogger<SinchChannel>.Instance);

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
            .ThrowsAsync(new HttpRequestException("Timeout"));

        var channel = new SinchChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<SinchChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Timeout", result.Error);
    }

    // ── bulk send ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_Success_ReturnsAllSucceeded()
    {
        var response = new
        {
            id = "sinch-batch-bulk-123",
            type = "mt_text"
        };

        var payloads = new[]
        {
            new NotificationPayload { To = "+447700900001", Body = "Hi" },
            new NotificationPayload { To = "+447700900002", Body = "Hi" }
        };

        var channel = new SinchChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Created, response),
            NullLogger<SinchChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, result.Total);
        Assert.True(result.UsedNativeBatch);
        Assert.Equal("sms", result.Channel);
    }

    [Fact]
    public async Task SendBulkAsync_ChunkFails_MarksAllInChunkFailed()
    {
        var payloads = new[]
        {
            new NotificationPayload { To = "+447700900001", Body = "Hi" },
            new NotificationPayload { To = "+447700900002", Body = "Hi" }
        };

        var channel = new SinchChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Unauthorized, new { code = 401, text = "Unauthorized" }),
            NullLogger<SinchChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.AllSucceeded);
        Assert.Equal(2, result.FailureCount);
    }

    [Fact]
    public async Task SendBulkAsync_SetsSharedMessageIdAcrossRecipients()
    {
        var response = new
        {
            id = "shared-batch-id",
            type = "mt_text"
        };

        var payloads = new[]
        {
            new NotificationPayload { To = "+447700900001", Body = "Hi" },
            new NotificationPayload { To = "+447700900002", Body = "Hi" }
        };

        var channel = new SinchChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Created, response),
            NullLogger<SinchChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.All(result.Results, r => Assert.Equal("shared-batch-id", r.ProviderId));
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsSms()
    {
        var channel = new SinchChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<SinchChannel>.Instance);

        Assert.Equal("sms", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var response = new { id = "batch-1", type = "mt_text" };

        var channel = new SinchChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Created, response),
            NullLogger<SinchChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_SetsChannelAndProvider()
    {
        var response = new { id = "batch-1", type = "mt_text" };

        var channel = new SinchChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Created, response),
            NullLogger<SinchChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("sms", result.Channel);
        Assert.Equal("sinch", result.Provider);
    }
}
