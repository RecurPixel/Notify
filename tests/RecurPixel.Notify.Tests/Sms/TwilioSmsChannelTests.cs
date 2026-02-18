using RecurPixel.Notify.Sms.Twilio;

namespace RecurPixel.Notify.Tests.Sms;

public class TwilioSmsChannelTests
{
    private static TwilioSmsChannel BuildChannel() =>
        new TwilioSmsChannel(
            Options.Create(new TwilioOptions
            {
                AccountSid = "ACinvalid",
                AuthToken = "invalid",
                FromNumber = "+10000000000"
            }),
            NullLogger<TwilioSmsChannel>.Instance);

    // ── ChannelName ───────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_Returns_Sms()
    {
        var channel = BuildChannel();
        Assert.Equal("sms", channel.ChannelName);
    }

    // ── SendAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ReturnsFailure_WhenCredentials_AreInvalid()
    {
        var channel = BuildChannel();

        var result = await channel.SendAsync(new NotificationPayload
        {
            To = "+919999999999",
            Body = "Test message"
        });

        Assert.False(result.Success);
        Assert.Equal("sms", result.Channel);
        Assert.Equal("twilio", result.Provider);
        Assert.Equal("+919999999999", result.Recipient);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task SendAsync_SetsRecipient_FromPayloadTo()
    {
        var channel = BuildChannel();

        var result = await channel.SendAsync(new NotificationPayload
        {
            To = "+919999999999",
            Body = "Test message"
        });

        Assert.Equal("+919999999999", result.Recipient);
    }

    [Fact]
    public async Task SendAsync_NeverThrows_OnException()
    {
        var channel = BuildChannel();

        var ex = await Record.ExceptionAsync(() => channel.SendAsync(new NotificationPayload
        {
            To = "+919999999999",
            Body = "Test message"
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
            new() { To = "+911111111111", Body = "Hi" },
            new() { To = "+912222222222", Body = "Hi" },
            new() { To = "+913333333333", Body = "Hi" },
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.Equal(3, result.Total);
        Assert.Equal("sms", result.Channel);
    }

    [Fact]
    public async Task SendBulkAsync_SetsUsedNativeBatch_False()
    {
        // Twilio has no native SMS bulk — base class loop runs
        var channel = BuildChannel();
        var payloads = new List<NotificationPayload>
        {
            new() { To = "+911111111111", Body = "Hi" }
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.UsedNativeBatch);
    }

    [Fact]
    public async Task SendBulkAsync_SetsRecipient_OnEachResult()
    {
        var channel = BuildChannel();
        var payloads = new List<NotificationPayload>
        {
            new() { To = "+911111111111", Body = "Hi" },
            new() { To = "+912222222222", Body = "Hi" },
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.Equal("+911111111111", result.Results[0].Recipient);
        Assert.Equal("+912222222222", result.Results[1].Recipient);
    }

    [Fact]
    public async Task SendBulkAsync_NeverThrows_OnException()
    {
        var channel = BuildChannel();
        var payloads = new List<NotificationPayload>
        {
            new() { To = "+911111111111", Body = "Hi" }
        };

        var ex = await Record.ExceptionAsync(() => channel.SendBulkAsync(payloads));

        Assert.Null(ex);
    }
}