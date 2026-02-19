using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Sms.Twilio;

namespace RecurPixel.Notify.IntegrationTests.Sms;

/// <summary>
/// Integration tests for the Twilio SMS adapter.
/// Twilio has no native bulk SMS API — base class loop handles bulk automatically.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Sms:Twilio:AccountSid
///   Notify:Sms:Twilio:AuthToken
///   Notify:Sms:Twilio:FromNumber   (format: +12345678900)
///   Integration:ToPhone            (format: +447700900000)
///
/// NOTE: Twilio free trial restricts sending to verified numbers only.
///       Verify your test phone at console.twilio.com before running.
/// </summary>
public sealed class TwilioSmsIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "sms:twilio";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Sms:Twilio:AccountSid",
            "Notify:Sms:Twilio:AuthToken",
            "Notify:Sms:Twilio:FromNumber",
            "Integration:ToPhone");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new TwilioOptions
        {
            AccountSid = config["Notify:Sms:Twilio:AccountSid"]!,
            AuthToken = config["Notify:Sms:Twilio:AuthToken"]!,
            FromNumber = config["Notify:Sms:Twilio:FromNumber"]!
        };
        services.AddTwilioSmsChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToPhone"]!,
        Body = $"RecurPixel.Notify integration test — Twilio SMS — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
