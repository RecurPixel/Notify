using RecurPixel.Notify.Email.Postmark;

namespace RecurPixel.Notify.Tests;

public sealed class PostmarkChannelTests
{
    private static PostmarkOptions DefaultOptions => new()
    {
        ApiKey = "test-postmark-token",
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

    // ── single send ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_Success_ReturnsTrueWithMessageId()
    {
        var response = new
        {
            MessageID = "postmark-msg-abc123",
            ErrorCode = 0,
            Message = "OK"
        };

        var channel = new PostmarkChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<PostmarkChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("email", result.Channel);
        Assert.Equal("postmark", result.Provider);
        Assert.Equal("postmark-msg-abc123", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_SetsPostmarkServerTokenHeader()
    {
        var handler = new Mock<HttpMessageHandler>();
        string? capturedToken = null;

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                req.Headers.TryGetValues("X-Postmark-Server-Token", out var vals);
                capturedToken = vals is not null ? string.Join(",", vals) : null;
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    MessageID = "abc",
                    ErrorCode = 0,
                    Message = "OK"
                }))
            });

        var channel = new PostmarkChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<PostmarkChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.Equal("test-postmark-token", capturedToken);
    }

    [Fact]
    public async Task SendAsync_HtmlBody_SendsAsHtml()
    {
        var payload = new NotificationPayload
        {
            To = "recipient@example.com",
            Subject = "Hello",
            Body = "<h1>Hello</h1>"
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
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    MessageID = "abc",
                    ErrorCode = 0,
                    Message = "OK"
                }))
            });

        var channel = new PostmarkChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<PostmarkChannel>.Instance);

        await channel.SendAsync(payload);

        Assert.NotNull(capturedBody);
        Assert.Contains("HtmlBody", capturedBody);
        Assert.DoesNotContain("TextBody", capturedBody);
    }

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalse()
    {
        var channel = new PostmarkChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.UnprocessableEntity, new { Message = "Invalid" }),
            NullLogger<PostmarkChannel>.Instance);

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
            .ThrowsAsync(new HttpRequestException("Timeout"));

        var channel = new PostmarkChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<PostmarkChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Timeout", result.Error);
    }

    // ── bulk send ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_Success_ReturnsAllSucceeded()
    {
        var batchResponse = new[]
        {
            new { MessageID = "msg-1", ErrorCode = 0, Message = "OK" },
            new { MessageID = "msg-2", ErrorCode = 0, Message = "OK" }
        };

        var payloads = new[]
        {
            new NotificationPayload { To = "a@example.com", Subject = "Hi", Body = "Body" },
            new NotificationPayload { To = "b@example.com", Subject = "Hi", Body = "Body" }
        };

        var channel = new PostmarkChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, batchResponse),
            NullLogger<PostmarkChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, result.Total);
        Assert.True(result.UsedNativeBatch);
        Assert.Equal("email", result.Channel);
    }

    [Fact]
    public async Task SendBulkAsync_PartialErrorCode_MarksFailedResults()
    {
        var batchResponse = new[]
        {
            new { MessageID = "msg-1", ErrorCode = 0,   Message = "OK" },
            new { MessageID = (string?)null!, ErrorCode = 406, Message = "Invalid address" }
        };

        var payloads = new[]
        {
            new NotificationPayload { To = "a@example.com", Subject = "Hi", Body = "Body" },
            new NotificationPayload { To = "bad@",          Subject = "Hi", Body = "Body" }
        };

        var channel = new PostmarkChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, batchResponse),
            NullLogger<PostmarkChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.AllSucceeded);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Contains("406", result.Failures[0].Error);
    }

    [Fact]
    public async Task SendBulkAsync_ChunkFails_MarksAllInChunkFailed()
    {
        var payloads = new[]
        {
            new NotificationPayload { To = "a@example.com", Subject = "Hi", Body = "Body" },
            new NotificationPayload { To = "b@example.com", Subject = "Hi", Body = "Body" }
        };

        var channel = new PostmarkChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Unauthorized, new { Message = "Unauthorized" }),
            NullLogger<PostmarkChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.AllSucceeded);
        Assert.Equal(2, result.FailureCount);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsEmail()
    {
        var channel = new PostmarkChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<PostmarkChannel>.Instance);

        Assert.Equal("email", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var response = new { MessageID = "abc", ErrorCode = 0, Message = "OK" };

        var channel = new PostmarkChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<PostmarkChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }
}
