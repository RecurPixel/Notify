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
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Telegram;
using Xunit;

namespace RecurPixel.Notify.Tests;

public sealed class TelegramChannelTests
{
    private static TelegramOptions DefaultOptions => new()
    {
        BotToken = "test-bot-token",
        ParseMode = null
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To = "123456789",
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
    public async Task SendAsync_Success_ReturnsTrueWithMessageId()
    {
        var response = new
        {
            ok = true,
            result = new { message_id = 42 }
        };

        var channel = new TelegramChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<TelegramChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("telegram", result.Channel);
        Assert.Equal("telegram", result.Provider);
        Assert.Equal("42", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_NoSubject_SendsBodyOnly()
    {
        var payload = new NotificationPayload
        {
            To = "123456789",
            Subject = "",
            Body = "Body only"
        };

        var response = new
        {
            ok = true,
            result = new { message_id = 99 }
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
                Content = new StringContent(JsonSerializer.Serialize(response))
            });

        var channel = new TelegramChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<TelegramChannel>.Instance);

        var result = await channel.SendAsync(payload);

        Assert.True(result.Success);
        Assert.NotNull(capturedBody);
        Assert.Contains("Body only", capturedBody);
        Assert.DoesNotContain("\n\n", capturedBody);
    }

    // ── failure ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_NonSuccessStatusCode_ReturnsFalseWithError()
    {
        var response = new { description = "Bad Request: chat not found" };

        var channel = new TelegramChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.BadRequest, response),
            NullLogger<TelegramChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Equal("telegram", result.Channel);
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
            .ThrowsAsync(new HttpRequestException("Network failure"));

        var channel = new TelegramChannel(
            Options.Create(DefaultOptions),
            new HttpClient(handler.Object),
            NullLogger<TelegramChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Network failure", result.Error);
        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsTelegram()
    {
        var channel = new TelegramChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, new { }),
            NullLogger<TelegramChannel>.Instance);

        Assert.Equal("telegram", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var response = new
        {
            ok = true,
            result = new { message_id = 1 }
        };

        var channel = new TelegramChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<TelegramChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_SetsChannelAndProvider()
    {
        var response = new
        {
            ok = true,
            result = new { message_id = 1 }
        };

        var channel = new TelegramChannel(
            Options.Create(DefaultOptions),
            MakeClient(HttpStatusCode.OK, response),
            NullLogger<TelegramChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("telegram", result.Channel);
        Assert.Equal("telegram", result.Provider);
    }
}
