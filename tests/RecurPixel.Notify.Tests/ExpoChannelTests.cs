using RecurPixel.Notify.Push.Expo;

namespace RecurPixel.Notify.Tests;

public sealed class ExpoChannelTests
{
    private static ExpoOptions DefaultOptions => new()
    {
        AccessToken = "test-access-token"
    };

    private static ExpoOptions NoTokenOptions => new()
    {
        AccessToken = null
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To = "ExponentPushToken[abc123]",
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
    public async Task SendAsync_Success_ReturnsTrueWithTicketId()
    {
        var response = new
        {
            data = new[]
            {
                new { status = "ok", id = "expo-ticket-abc123" }
            }
        };

        var channel = new ExpoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<ExpoChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("push", result.Channel);
        Assert.Equal("expo", result.Provider);
        Assert.Equal("expo-ticket-abc123", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_TicketStatusError_ReturnsFalse()
    {
        var response = new
        {
            data = new[]
            {
                new
                {
                    status  = "error",
                    id      = (string?)null,
                    message = "DeviceNotRegistered",
                    details = new { error = "DeviceNotRegistered" }
                }
            }
        };

        var channel = new ExpoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<ExpoChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Equal("push", result.Channel);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Contains("DeviceNotRegistered", result.Error);
    }

    [Fact]
    public async Task SendAsync_SetsBearerAuthHeader_WhenAccessTokenPresent()
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
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    data = new[] { new { status = "ok", id = "ticket-1" } }
                }))
            });

        var channel = new ExpoChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<ExpoChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(capturedAuth);
        Assert.Contains("Bearer test-access-token", capturedAuth);
    }

    [Fact]
    public async Task SendAsync_NoAccessToken_OmitsAuthHeader()
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
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    data = new[] { new { status = "ok", id = "ticket-1" } }
                }))
            });

        var channel = new ExpoChannel(
            Options.Create(NoTokenOptions),
            new HttpClient(handler.Object),
            NullLogger<ExpoChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.Null(capturedAuth);
    }

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalse()
    {
        var channel = new ExpoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.TooManyRequests, new { error = "Rate limit exceeded" }),
            NullLogger<ExpoChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("429", result.Error);
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

        var channel = new ExpoChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<ExpoChannel>.Instance);

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
            data = new[]
            {
                new { status = "ok", id = "ticket-1" },
                new { status = "ok", id = "ticket-2" }
            }
        };

        var payloads = new[]
        {
            new NotificationPayload { To = "ExponentPushToken[token1]", Subject = "Hi", Body = "Body" },
            new NotificationPayload { To = "ExponentPushToken[token2]", Subject = "Hi", Body = "Body" }
        };

        var channel = new ExpoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<ExpoChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, result.Total);
        Assert.True(result.UsedNativeBatch);
        Assert.Equal("push", result.Channel);
    }

    [Fact]
    public async Task SendBulkAsync_PartialTicketError_MarksFailedResults()
    {
        var response = new
        {
            data = new object[]
            {
                new { status = "ok",    id = "ticket-1", message = (string?)null,        details = (object?)null },
                new { status = "error", id = (string?)null, message = "DeviceNotRegistered",
                      details = new { error = "DeviceNotRegistered" } }
            }
        };

        var payloads = new[]
        {
            new NotificationPayload { To = "ExponentPushToken[good]",  Subject = "Hi", Body = "Body" },
            new NotificationPayload { To = "ExponentPushToken[bad]",   Subject = "Hi", Body = "Body" }
        };

        var channel = new ExpoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<ExpoChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.AllSucceeded);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Contains("DeviceNotRegistered", result.Failures[0].Error);
    }

    [Fact]
    public async Task SendBulkAsync_ChunkFails_MarksAllInChunkFailed()
    {
        var payloads = new[]
        {
            new NotificationPayload { To = "ExponentPushToken[t1]", Subject = "Hi", Body = "Body" },
            new NotificationPayload { To = "ExponentPushToken[t2]", Subject = "Hi", Body = "Body" }
        };

        var channel = new ExpoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.TooManyRequests, new { error = "Rate limit" }),
            NullLogger<ExpoChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.AllSucceeded);
        Assert.Equal(2, result.FailureCount);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsPush()
    {
        var channel = new ExpoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<ExpoChannel>.Instance);

        Assert.Equal("push", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var response = new
        {
            data = new[] { new { status = "ok", id = "ticket-1" } }
        };

        var channel = new ExpoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<ExpoChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_SetsChannelAndProvider()
    {
        var response = new
        {
            data = new[] { new { status = "ok", id = "ticket-1" } }
        };

        var channel = new ExpoChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<ExpoChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("push", result.Channel);
        Assert.Equal("expo", result.Provider);
    }
}
