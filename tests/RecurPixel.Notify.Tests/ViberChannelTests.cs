using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Viber;
using Xunit;

namespace RecurPixel.Notify.Tests;

public sealed class ViberChannelTests
{
    private static ViberOptions DefaultOptions => new()
    {
        BotAuthToken    = "test-bot-auth-token",
        SenderName      = "TestBot",
        SenderAvatarUrl = null
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To      = "viber-user-id-abc123",
        Subject = "Hello",
        Body    = "World"
    };

    private static HttpClient MakeClient(HttpStatusCode status, object responseBody)
    {
        var json    = JsonSerializer.Serialize(responseBody);
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
                Content    = new StringContent(json)
            });

        return new HttpClient(handler.Object);
    }

    // ── success ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_Success_ReturnsTrueWithMessageToken()
    {
        var response = new
        {
            status         = 0,
            status_message = "ok",
            message_token  = 5098034272012345678L
        };

        var channel = new ViberChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<ViberChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("viber", result.Channel);
        Assert.Equal("viber", result.Provider);
        Assert.Equal("5098034272012345678", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_NoSubject_SendsBodyOnly()
    {
        var payload = new NotificationPayload
        {
            To      = "viber-user-id-abc123",
            Subject = "",
            Body    = "Body only"
        };

        var response = new
        {
            status         = 0,
            status_message = "ok",
            message_token  = 111L
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
                Content    = new StringContent(JsonSerializer.Serialize(response))
            });

        var channel = new ViberChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<ViberChannel>.Instance);

        var result = await channel.SendAsync(payload);

        Assert.True(result.Success);
        Assert.NotNull(capturedBody);
        Assert.Contains("Body only", capturedBody);
        Assert.DoesNotContain("\n\n", capturedBody);
    }

    [Fact]
    public async Task SendAsync_SetsAuthTokenHeader()
    {
        var response = new
        {
            status         = 0,
            status_message = "ok",
            message_token  = 111L
        };

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
                req.Headers.TryGetValues("X-Viber-Auth-Token", out var values);
                capturedToken = values is not null ? string.Join(",", values) : null;
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content    = new StringContent(JsonSerializer.Serialize(response))
            });

        var channel = new ViberChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<ViberChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.Equal("test-bot-auth-token", capturedToken);
    }

    // ── Viber-specific: non-zero status on HTTP 200 ───────────────────────────

    [Fact]
    public async Task SendAsync_ViberStatusNonZero_ReturnsFalseWithError()
    {
        var response = new
        {
            status         = 6,
            status_message = "Not subscribed",
            message_token  = 0L
        };

        var channel = new ViberChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<ViberChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Equal("viber", result.Channel);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Contains("6", result.Error);
        Assert.Contains("Not subscribed", result.Error);
    }

    // ── failure ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalseWithError()
    {
        var response = new { error = "Unauthorized" };

        var channel = new ViberChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.Unauthorized, response),
            NullLogger<ViberChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Equal("viber", result.Channel);
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
            .ThrowsAsync(new HttpRequestException("DNS failure"));

        var channel = new ViberChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<ViberChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("DNS failure", result.Error);
        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsViber()
    {
        var channel = new ViberChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<ViberChannel>.Instance);

        Assert.Equal("viber", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var response = new
        {
            status         = 0,
            status_message = "ok",
            message_token  = 999L
        };

        var channel = new ViberChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<ViberChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_SetsChannelAndProvider()
    {
        var response = new
        {
            status         = 0,
            status_message = "ok",
            message_token  = 999L
        };

        var channel = new ViberChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<ViberChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("viber", result.Channel);
        Assert.Equal("viber", result.Provider);
    }
}
