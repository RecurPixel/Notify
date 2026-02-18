using System;
using RecurPixel.Notify.Core.Extensions;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Channels;
using RecurPixel.Notify.Core.Options.Providers;
using Xunit;

namespace RecurPixel.Notify.Tests.Core;

public class NotifyOptionsValidatorTests
{
    // ── Email validation ──────────────────────────────────────────────────────

    [Fact]
    public void Validate_Throws_WhenEmailProvider_IsMissing()
    {
        var options = new NotifyOptions
        {
            Email = new EmailOptions { Provider = null }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            NotifyOptionsValidator.Validate(options));

        Assert.Contains("Email:Provider", ex.Message);
    }

    [Fact]
    public void Validate_Throws_WhenSendGrid_ApiKey_IsMissing()
    {
        var options = new NotifyOptions
        {
            Email = new EmailOptions
            {
                Provider = "sendgrid",
                SendGrid = new SendGridOptions { ApiKey = null }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            NotifyOptionsValidator.Validate(options));

        Assert.Contains("SendGrid:ApiKey", ex.Message);
    }

    [Fact]
    public void Validate_Throws_WhenSmtp_Host_IsMissing()
    {
        var options = new NotifyOptions
        {
            Email = new EmailOptions
            {
                Provider = "smtp",
                Smtp = new SmtpOptions { Host = null }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            NotifyOptionsValidator.Validate(options));

        Assert.Contains("Smtp:Host", ex.Message);
    }

    [Fact]
    public void Validate_DoesNotThrow_WhenEmailIsNull()
    {
        // Email not configured at all — valid, user just isn't using email
        var options = new NotifyOptions { Email = null };

        var ex = Record.Exception(() => NotifyOptionsValidator.Validate(options));

        Assert.Null(ex);
    }

    [Fact]
    public void Validate_DoesNotThrow_WhenSendGrid_IsFullyConfigured()
    {
        var options = new NotifyOptions
        {
            Email = new EmailOptions
            {
                Provider = "sendgrid",
                SendGrid = new SendGridOptions
                {
                    ApiKey = "SG.test",
                    FromEmail = "no-reply@test.com",
                    FromName = "Test"
                }
            }
        };

        var ex = Record.Exception(() => NotifyOptionsValidator.Validate(options));

        Assert.Null(ex);
    }

    // ── SMS validation ────────────────────────────────────────────────────────

    [Fact]
    public void Validate_Throws_WhenSmsProvider_IsMissing()
    {
        var options = new NotifyOptions
        {
            Sms = new SmsOptions { Provider = null }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            NotifyOptionsValidator.Validate(options));

        Assert.Contains("Sms:Provider", ex.Message);
    }

    [Fact]
    public void Validate_Throws_WhenTwilio_AccountSid_IsMissing()
    {
        var options = new NotifyOptions
        {
            Sms = new SmsOptions
            {
                Provider = "twilio",
                Twilio = new TwilioOptions { AccountSid = null, AuthToken = "x", FromNumber = "+1" }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            NotifyOptionsValidator.Validate(options));

        Assert.Contains("Twilio:AccountSid", ex.Message);
    }
}