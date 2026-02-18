using RecurPixel.Notify.Email.Mailgun;

namespace RecurPixel.Notify.Tests;

public sealed class MailgunChannelTests
{
    private static MailgunOptions DefaultOptions => new()
    {
        ApiKey = "test-api-key",
        Domain = "test.mailgun.org",
        FromEmail = "no-reply@test.mailgun.org",
        FromName = "Test"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To = "recipient@example.com",
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

    [Fact]
    public async Task SendAsync_Success_ReturnsTrueWithMessageId()
    {
        var response = new { id = "<msg-id-123@mailgun.org>", message = "Queued" };

        var channel = new MailgunChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<MailgunChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("email", result.Channel);
        Assert.Equal("mailgun", result.Provider);
        Assert.Equal("<msg-id-123@mailgun.org>", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalse()
    {
        var channel = new MailgunChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Unauthorized, new { message = "Forbidden" }),
            NullLogger<MailgunChannel>.Instance);

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

        var channel = new MailgunChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<MailgunChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Network error", result.Error);
    }

    [Fact]
    public async Task SendBulkAsync_Success_ReturnsAllSucceeded()
    {
        var response = new { id = "<bulk-id@mailgun.org>", message = "Queued" };

        var payloads = new[]
        {
            new NotificationPayload { To = "a@example.com", Subject = "Hi", Body = "Body" },
            new NotificationPayload { To = "b@example.com", Subject = "Hi", Body = "Body" }
        };

        var channel = new MailgunChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<MailgunChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, result.Total);
        Assert.True(result.UsedNativeBatch);
        Assert.Equal("email", result.Channel);
    }

    [Fact]
    public async Task SendBulkAsync_ChunkFails_MarksAllInChunkFailed()
    {
        var payloads = new[]
        {
            new NotificationPayload { To = "a@example.com", Subject = "Hi", Body = "Body" },
            new NotificationPayload { To = "b@example.com", Subject = "Hi", Body = "Body" }
        };

        var channel = new MailgunChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Unauthorized, new { message = "Forbidden" }),
            NullLogger<MailgunChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.AllSucceeded);
        Assert.Equal(2, result.FailureCount);
    }

    [Fact]
    public void ChannelName_IsEmail()
    {
        var channel = new MailgunChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<MailgunChannel>.Instance);

        Assert.Equal("email", channel.ChannelName);
    }
}
