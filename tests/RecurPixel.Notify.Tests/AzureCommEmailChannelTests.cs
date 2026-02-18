using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Email.AzureCommEmail;
using Xunit;

namespace RecurPixel.Notify.Tests;

public sealed class AzureCommEmailChannelTests
{
    private static AzureCommEmailOptions DefaultOptions => new()
    {
        ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=dGVzdA==",
        FromEmail        = "no-reply@test.com",
        FromName         = "Test"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To      = "recipient@example.com",
        Subject = "Hello",
        Body    = "Plain text body"
    };

    private static Mock<IAzureCommEmailClient> MakeClientMock(
        bool succeeds,
        string messageId = "acs-email-msg-abc123")
    {
        var mock = new Mock<IAzureCommEmailClient>();

        if (succeeds)
        {
            mock.Setup(c => c.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(messageId);
        }
        else
        {
            mock.Setup(c => c.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service unavailable"));
        }

        return mock;
    }

    // ── success ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_Success_ReturnsTrueWithMessageId()
    {
        var mock    = MakeClientMock(succeeds: true, "acs-email-msg-abc123");
        var channel = new AzureCommEmailChannel(
            Options.Create(DefaultOptions),
            mock.Object,
            NullLogger<AzureCommEmailChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("email", result.Channel);
        Assert.Equal("azurecommemail", result.Provider);
        Assert.Equal("acs-email-msg-abc123", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_Exception_ReturnsFalse()
    {
        var mock    = MakeClientMock(succeeds: false);
        var channel = new AzureCommEmailChannel(
            Options.Create(DefaultOptions),
            mock.Object,
            NullLogger<AzureCommEmailChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Equal("email", result.Channel);
        Assert.Equal("azurecommemail", result.Provider);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Contains("Service unavailable", result.Error);
    }

    [Fact]
    public async Task SendAsync_PlainTextBody_PassesAsPlainText()
    {
        string? capturedHtml      = "not-set";
        string? capturedPlainText = "not-set";

        var mock = new Mock<IAzureCommEmailClient>();
        mock.Setup(c => c.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string?, string?, CancellationToken>(
                (_, _, _, html, plain, _) =>
                {
                    capturedHtml      = html;
                    capturedPlainText = plain;
                })
            .ReturnsAsync("msg-1");

        var channel = new AzureCommEmailChannel(
            Options.Create(DefaultOptions),
            mock.Object,
            NullLogger<AzureCommEmailChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.Null(capturedHtml);
        Assert.Equal("Plain text body", capturedPlainText);
    }

    [Fact]
    public async Task SendAsync_HtmlBody_PassesAsHtml()
    {
        var payload = new NotificationPayload
        {
            To      = "recipient@example.com",
            Subject = "Hello",
            Body    = "<h1>Hello</h1>"
        };

        string? capturedHtml      = "not-set";
        string? capturedPlainText = "not-set";

        var mock = new Mock<IAzureCommEmailClient>();
        mock.Setup(c => c.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string?, string?, CancellationToken>(
                (_, _, _, html, plain, _) =>
                {
                    capturedHtml      = html;
                    capturedPlainText = plain;
                })
            .ReturnsAsync("msg-1");

        var channel = new AzureCommEmailChannel(
            Options.Create(DefaultOptions),
            mock.Object,
            NullLogger<AzureCommEmailChannel>.Instance);

        await channel.SendAsync(payload);

        Assert.Equal("<h1>Hello</h1>", capturedHtml);
        Assert.Null(capturedPlainText);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsEmail()
    {
        var channel = new AzureCommEmailChannel(
            Options.Create(DefaultOptions),
            MakeClientMock(succeeds: true).Object,
            NullLogger<AzureCommEmailChannel>.Instance);

        Assert.Equal("email", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsRecipientOnResult()
    {
        var channel = new AzureCommEmailChannel(
            Options.Create(DefaultOptions),
            MakeClientMock(succeeds: true).Object,
            NullLogger<AzureCommEmailChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }
}
