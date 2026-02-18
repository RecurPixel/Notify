using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Email.Smtp;
using Xunit;

namespace RecurPixel.Notify.Tests.Email;

public class SmtpChannelTests
{
    private static SmtpChannel BuildChannel() =>
        new SmtpChannel(
            Options.Create(new SmtpOptions
            {
                Host = "invalid.smtp.host",
                Port = 587,
                Username = "user",
                Password = "pass",
                UseSsl = true,
                FromEmail = "no-reply@test.com",
                FromName = "Test"
            }),
            NullLogger<SmtpChannel>.Instance);

    // ── ChannelName ───────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_Returns_Email()
    {
        var channel = BuildChannel();
        Assert.Equal("email", channel.ChannelName);
    }

    // ── SendAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ReturnsFailure_WhenHost_IsInvalid()
    {
        // Uses a deliberately invalid host — connection will fail.
        // We are testing that the adapter catches it and returns
        // NotifyResult rather than throwing.
        var channel = BuildChannel();

        var result = await channel.SendAsync(new NotificationPayload
        {
            To = "user@example.com",
            Subject = "Test",
            Body = "<p>Hello</p>"
        });

        Assert.False(result.Success);
        Assert.Equal("email", result.Channel);
        Assert.Equal("smtp", result.Provider);
        Assert.Equal("user@example.com", result.Recipient);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task SendAsync_SetsRecipient_FromPayloadTo()
    {
        var channel = BuildChannel();

        var result = await channel.SendAsync(new NotificationPayload
        {
            To = "someone@example.com",
            Subject = "Hi",
            Body = "Body"
        });

        Assert.Equal("someone@example.com", result.Recipient);
    }

    [Fact]
    public async Task SendAsync_NeverThrows_OnException()
    {
        var channel = BuildChannel();

        var ex = await Record.ExceptionAsync(() => channel.SendAsync(new NotificationPayload
        {
            To = "user@example.com",
            Subject = "Test",
            Body = "Body"
        }));

        Assert.Null(ex);
    }

    // ── SendBulkAsync (base class loop) ───────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_ReturnsOneResult_PerPayload()
    {
        var channel = BuildChannel();
        var payloads = new List<NotificationPayload>
        {
            new() { To = "a@example.com", Subject = "Hi", Body = "Body" },
            new() { To = "b@example.com", Subject = "Hi", Body = "Body" },
            new() { To = "c@example.com", Subject = "Hi", Body = "Body" },
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.Equal(3, result.Total);
        Assert.Equal("email", result.Channel);
    }

    [Fact]
    public async Task SendBulkAsync_SetsUsedNativeBatch_False()
    {
        // SMTP has no native bulk — base class loop runs, UsedNativeBatch must be false
        var channel = BuildChannel();
        var payloads = new List<NotificationPayload>
        {
            new() { To = "a@example.com", Subject = "Hi", Body = "Body" }
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.UsedNativeBatch);
    }

    [Fact]
    public async Task SendBulkAsync_NeverThrows_OnException()
    {
        var channel = BuildChannel();
        var payloads = new List<NotificationPayload>
        {
            new() { To = "a@example.com", Subject = "Hi", Body = "Body" }
        };

        var ex = await Record.ExceptionAsync(() => channel.SendBulkAsync(payloads));

        Assert.Null(ex);
    }

    [Fact]
    public async Task SendBulkAsync_SetsRecipient_OnEachResult()
    {
        var channel = BuildChannel();
        var payloads = new List<NotificationPayload>
        {
            new() { To = "a@example.com", Body = "Hi" },
            new() { To = "b@example.com", Body = "Hi" },
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.Equal("a@example.com", result.Results[0].Recipient);
        Assert.Equal("b@example.com", result.Results[1].Recipient);
    }
}