using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Email.SendGrid;

namespace RecurPixel.Notify.Tests.Email;

public class SendGridChannelTests
{
    private static SendGridChannel BuildChannel(string apiKey = "SG.test") =>
        new SendGridChannel(
            Options.Create(new SendGridOptions
            {
                ApiKey = apiKey,
                FromEmail = "no-reply@test.com",
                FromName = "Test"
            }),
            NullLogger<SendGridChannel>.Instance);

    // ── ChannelName ───────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_Returns_Email()
    {
        var channel = BuildChannel();
        Assert.Equal("email", channel.ChannelName);
    }

    // ── SendAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ReturnsFailure_WhenApiKey_IsInvalid()
    {
        // Uses a deliberately bad key — SendGrid will reject it.
        // We are testing that the adapter catches the failure and
        // returns a NotifyResult rather than throwing.
        var channel = BuildChannel(apiKey: "SG.invalid");

        var result = await channel.SendAsync(new NotificationPayload
        {
            To = "user@example.com",
            Subject = "Test",
            Body = "<p>Hello</p>"
        });

        Assert.False(result.Success);
        Assert.Equal("email", result.Channel);
        Assert.Equal("sendgrid", result.Provider);
        Assert.Equal("user@example.com", result.Recipient);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task SendAsync_SetsRecipient_FromPayloadTo()
    {
        var channel = BuildChannel(apiKey: "SG.invalid");

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
        var channel = BuildChannel(apiKey: "SG.invalid");

        // Should not throw — all exceptions must be caught and returned as NotifyResult
        var ex = await Record.ExceptionAsync(() => channel.SendAsync(new NotificationPayload
        {
            To = "user@example.com",
            Subject = "Test",
            Body = "Body"
        }));

        Assert.Null(ex);
    }

    // ── SendBulkAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_ReturnsOneResult_PerPayload()
    {
        var channel = BuildChannel(apiKey: "SG.invalid");
        var payloads = new List<NotificationPayload>
        {
            new() { To = "a@example.com", Subject = "Hi", Body = "<p>A</p>" },
            new() { To = "b@example.com", Subject = "Hi", Body = "<p>B</p>" },
            new() { To = "c@example.com", Subject = "Hi", Body = "<p>C</p>" },
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.Equal(3, result.Total);
        Assert.Equal("email", result.Channel);
    }

    [Fact]
    public async Task SendBulkAsync_SetsUsedNativeBatch_True()
    {
        var channel = BuildChannel(apiKey: "SG.invalid");
        var payloads = new List<NotificationPayload>
        {
            new() { To = "a@example.com", Subject = "Hi", Body = "Body" }
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.True(result.UsedNativeBatch);
    }

    [Fact]
    public async Task SendBulkAsync_NeverThrows_OnException()
    {
        var channel = BuildChannel(apiKey: "SG.invalid");
        var payloads = new List<NotificationPayload>
        {
            new() { To = "a@example.com", Subject = "Hi", Body = "Body" }
        };

        var ex = await Record.ExceptionAsync(() => channel.SendBulkAsync(payloads));

        Assert.Null(ex);
    }

    [Fact]
    public async Task SendBulkAsync_EmptyList_ReturnsEmptyResult()
    {
        var channel = BuildChannel(apiKey: "SG.invalid");
        var payloads = new List<NotificationPayload>();

        var result = await channel.SendBulkAsync(payloads);

        Assert.Equal(0, result.Total);
    }
}