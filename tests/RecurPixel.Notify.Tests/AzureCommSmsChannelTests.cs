using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Sms.AzureCommSms;
using Xunit;

namespace RecurPixel.Notify.Tests;

public sealed class AzureCommSmsChannelTests
{
    private static AzureCommSmsOptions DefaultOptions => new()
    {
        ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=dGVzdA==",
        FromNumber       = "+15551234567"
    };

    private static NotificationPayload DefaultPayload => new()
    {
        To   = "+447700900000",
        Body = "Hello from ACS SMS"
    };

    private static Mock<IAzureCommSmsClient> MakeSingleMock(
        string to,
        bool succeeds,
        string messageId = "acs-sms-msg-abc123")
    {
        var mock   = new Mock<IAzureCommSmsClient>();
        var result = new AcsSmsResult(
            To:           to,
            MessageId:    succeeds ? messageId : null,
            Successful:   succeeds,
            StatusCode:   succeeds ? 202 : 400,
            ErrorMessage: succeeds ? null : "Invalid number");

        mock.Setup(c => c.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        return mock;
    }

    private static Mock<IAzureCommSmsClient> MakeBulkMock(
        IReadOnlyList<AcsSmsResult> results)
    {
        var mock = new Mock<IAzureCommSmsClient>();

        mock.Setup(c => c.SendBulkAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        return mock;
    }

    // ── success ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_Success_ReturnsTrueWithMessageId()
    {
        var mock    = MakeSingleMock(DefaultPayload.To, succeeds: true, "acs-sms-msg-abc123");
        var channel = new AzureCommSmsChannel(
            Options.Create(DefaultOptions),
            mock.Object,
            NullLogger<AzureCommSmsChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("sms", result.Channel);
        Assert.Equal("azurecommsms", result.Provider);
        Assert.Equal("acs-sms-msg-abc123", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_AcsReturnsUnsuccessful_ReturnsFalse()
    {
        var mock    = MakeSingleMock(DefaultPayload.To, succeeds: false);
        var channel = new AzureCommSmsChannel(
            Options.Create(DefaultOptions),
            mock.Object,
            NullLogger<AzureCommSmsChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Equal("sms", result.Channel);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Contains("400", result.Error);
    }

    [Fact]
    public async Task SendAsync_Exception_ReturnsFalse()
    {
        var mock = new Mock<IAzureCommSmsClient>();
        mock.Setup(c => c.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service unavailable"));

        var channel = new AzureCommSmsChannel(
            Options.Create(DefaultOptions),
            mock.Object,
            NullLogger<AzureCommSmsChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("Service unavailable", result.Error);
        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    // ── bulk send ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_AllSucceed_ReturnsAllSucceeded()
    {
        var results = new[]
        {
            new AcsSmsResult("+447700900001", "msg-1", true,  202, null),
            new AcsSmsResult("+447700900002", "msg-2", true,  202, null)
        };

        var mock    = MakeBulkMock(results);
        var payloads = new[]
        {
            new NotificationPayload { To = "+447700900001", Body = "Hi" },
            new NotificationPayload { To = "+447700900002", Body = "Hi" }
        };

        var channel = new AzureCommSmsChannel(
            Options.Create(DefaultOptions),
            mock.Object,
            NullLogger<AzureCommSmsChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, result.Total);
        Assert.True(result.UsedNativeBatch);
        Assert.Equal("sms", result.Channel);
    }

    [Fact]
    public async Task SendBulkAsync_PartialFailure_MarksFailedResults()
    {
        var results = new[]
        {
            new AcsSmsResult("+447700900001", "msg-1", true,  202, null),
            new AcsSmsResult("+447700900002", null,    false, 400, "Invalid number")
        };

        var mock    = MakeBulkMock(results);
        var payloads = new[]
        {
            new NotificationPayload { To = "+447700900001", Body = "Hi" },
            new NotificationPayload { To = "+447700900002", Body = "Hi" }
        };

        var channel = new AzureCommSmsChannel(
            Options.Create(DefaultOptions),
            mock.Object,
            NullLogger<AzureCommSmsChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.AllSucceeded);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Contains("400", result.Failures[0].Error);
    }

    [Fact]
    public async Task SendBulkAsync_Exception_MarksAllFailed()
    {
        var mock = new Mock<IAzureCommSmsClient>();
        mock.Setup(c => c.SendBulkAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service unavailable"));

        var payloads = new[]
        {
            new NotificationPayload { To = "+447700900001", Body = "Hi" },
            new NotificationPayload { To = "+447700900002", Body = "Hi" }
        };

        var channel = new AzureCommSmsChannel(
            Options.Create(DefaultOptions),
            mock.Object,
            NullLogger<AzureCommSmsChannel>.Instance);

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.AllSucceeded);
        Assert.Equal(2, result.FailureCount);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsSms()
    {
        var channel = new AzureCommSmsChannel(
            Options.Create(DefaultOptions),
            MakeSingleMock(DefaultPayload.To, succeeds: true).Object,
            NullLogger<AzureCommSmsChannel>.Instance);

        Assert.Equal("sms", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_SetsChannelAndProvider()
    {
        var channel = new AzureCommSmsChannel(
            Options.Create(DefaultOptions),
            MakeSingleMock(DefaultPayload.To, succeeds: true).Object,
            NullLogger<AzureCommSmsChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("sms", result.Channel);
        Assert.Equal("azurecommsms", result.Provider);
    }
}
