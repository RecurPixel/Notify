using System.Net.Http.Headers;
using RecurPixel.Notify.Line;

namespace RecurPixel.Notify.Tests;

public sealed class LineChannelTests
{
    private static LineOptions DefaultOptions => new()
    {
        ChannelAccessToken = "test-channel-access-token"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To = "line-user-id-abc123",
        Subject = "Hello",
        Body = "World"
    };

    private static HttpClient MakeClient(
        HttpStatusCode status,
        object responseBody,
        string? requestId = "line-request-id-xyz")
    {
        var json = JsonSerializer.Serialize(responseBody);
        var handler = new Mock<HttpMessageHandler>();

        var httpResponse = new HttpResponseMessage
        {
            StatusCode = status,
            Content = new StringContent(json)
        };

        if (requestId is not null)
            httpResponse.Headers.Add("X-Line-Request-Id", requestId);

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        return new HttpClient(handler.Object);
    }

    // ── success ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_Success_ReturnsTrueWithRequestId()
    {
        var channel = new LineChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }, "line-request-id-xyz"),
            NullLogger<LineChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("line", result.Channel);
        Assert.Equal("line", result.Provider);
        Assert.Equal("line-request-id-xyz", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_NoRequestIdHeader_ReturnsSuccessWithNullProviderId()
    {
        var channel = new LineChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }, requestId: null),
            NullLogger<LineChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Null(result.ProviderId);
    }

    [Fact]
    public async Task SendAsync_NoSubject_SendsBodyOnly()
    {
        var payload = new NotificationPayload
        {
            To = "line-user-id-abc123",
            Subject = "",
            Body = "Body only"
        };

        var handler = new Mock<HttpMessageHandler>();
        string? capturedBody = null;

        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{}")
        };
        httpResponse.Headers.Add("X-Line-Request-Id", "req-id-1");

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
            .ReturnsAsync(httpResponse);

        var channel = new LineChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<LineChannel>.Instance);

        var result = await channel.SendAsync(payload);

        Assert.True(result.Success);
        Assert.NotNull(capturedBody);
        Assert.Contains("Body only", capturedBody);
        Assert.DoesNotContain("\n\n", capturedBody);
    }

    [Fact]
    public async Task SendAsync_SetsAuthorizationHeader()
    {
        var handler = new Mock<HttpMessageHandler>();
        AuthenticationHeaderValue? capturedAuth = null;

        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{}")
        };
        httpResponse.Headers.Add("X-Line-Request-Id", "req-id-1");

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capturedAuth = req.Headers.Authorization;
            })
            .ReturnsAsync(httpResponse);

        var channel = new LineChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<LineChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(capturedAuth);
        Assert.Equal("Bearer", capturedAuth!.Scheme);
        Assert.Equal("test-channel-access-token", capturedAuth.Parameter);
    }

    // ── failure ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalseWithError()
    {
        var response = new { message = "The request body has 1 error(s)" };

        var channel = new LineChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.BadRequest, response, requestId: null),
            NullLogger<LineChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Equal("line", result.Channel);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Contains("400", result.Error);
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
            .ThrowsAsync(new HttpRequestException("Timeout"));

        var channel = new LineChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<LineChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Timeout", result.Error);
        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsLine()
    {
        var channel = new LineChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<LineChannel>.Instance);

        Assert.Equal("line", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var channel = new LineChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<LineChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_SetsChannelAndProvider()
    {
        var channel = new LineChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<LineChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("line", result.Channel);
        Assert.Equal("line", result.Provider);
    }
}
