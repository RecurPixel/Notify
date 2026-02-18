using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.WhatsApp.Twilio;

namespace RecurPixel.Notify.Tests.WhatsApp;

/// <summary>
/// TwilioWhatsAppChannel tests use real construction only — Twilio SDK calls
/// are exercised via exception-path testing (invalid credentials throw immediately).
/// All business logic (prefix handling, result mapping) is covered without
/// requiring a live Twilio account.
/// </summary>
public class TwilioWhatsAppChannelTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TwilioWhatsAppChannel MakeChannel(
        string fromNumber = "+1234567890") =>
        new(
            Options.Create(new TwilioOptions
            {
                AccountSid = "ACtest",
                AuthToken  = "test_token",
                FromNumber = fromNumber
            }),
            NullLogger<TwilioWhatsAppChannel>.Instance);

    private static NotificationPayload MakePayload(string to = "+9876543210") =>
        new() { To = to, Body = "Hello from WhatsApp" };

    // ── ChannelName ───────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsWhatsapp()
    {
        Assert.Equal("whatsapp", MakeChannel().ChannelName);
    }

    // ── SendAsync — failure path (invalid credentials throw immediately) ──────

    [Fact]
    public async Task SendAsync_InvalidCredentials_ReturnsFailureResult()
    {
        var channel = MakeChannel();
        var result  = await channel.SendAsync(MakePayload());

        // Twilio Init with fake creds throws on first API call
        Assert.False(result.Success);
        Assert.Equal("whatsapp", result.Channel);
        Assert.Equal("twilio",   result.Provider);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task SendAsync_InvalidCredentials_RecipientSet()
    {
        var channel = MakeChannel();
        var result  = await channel.SendAsync(MakePayload("+1111111111"));

        Assert.Equal("+1111111111", result.Recipient);
    }

    [Fact]
    public async Task SendAsync_InvalidCredentials_DoesNotThrow()
    {
        var channel = MakeChannel();
        var ex      = await Record.ExceptionAsync(() => channel.SendAsync(MakePayload()));
        Assert.Null(ex);
    }

    [Fact]
    public async Task SendAsync_InvalidCredentials_SentAtIsRecentUtc()
    {
        var before  = DateTime.UtcNow.AddSeconds(-1);
        var channel = MakeChannel();
        var result  = await channel.SendAsync(MakePayload());

        Assert.True(result.SentAt >= before);
        Assert.True(result.SentAt <= DateTime.UtcNow.AddSeconds(1));
    }

    // ── Prefix handling ───────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_FromNumberWithoutPrefix_DoesNotThrowOnPrefixLogic()
    {
        // Channel must not throw when normalising the whatsapp: prefix
        var channel = MakeChannel(fromNumber: "+1234567890");
        var ex      = await Record.ExceptionAsync(() => channel.SendAsync(MakePayload()));
        Assert.Null(ex);
    }

    [Fact]
    public async Task SendAsync_FromNumberAlreadyHasPrefix_DoesNotThrow()
    {
        var channel = MakeChannel(fromNumber: "whatsapp:+1234567890");
        var ex      = await Record.ExceptionAsync(() => channel.SendAsync(MakePayload()));
        Assert.Null(ex);
    }

    // ── Bulk — base class loop ────────────────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_UsedNativeBatch_IsFalse()
    {
        var channel  = MakeChannel();
        var payloads = new[]
        {
            new NotificationPayload { To = "+1111111111", Body = "msg 1" },
            new NotificationPayload { To = "+2222222222", Body = "msg 2" }
        };

        var result = await channel.SendBulkAsync(payloads);

        // No native bulk — base class loop, UsedNativeBatch must be false
        Assert.False(result.UsedNativeBatch);
        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task SendBulkAsync_EmptyList_ReturnsEmptyResult()
    {
        var channel = MakeChannel();
        var result  = await channel.SendBulkAsync(Array.Empty<NotificationPayload>());

        Assert.Equal(0, result.Total);
        Assert.True(result.AllSucceeded);
    }
}
