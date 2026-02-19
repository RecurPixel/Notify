using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.WhatsApp.Twilio;

namespace RecurPixel.Notify.IntegrationTests.WhatsApp;

/// <summary>
/// Integration tests for the Twilio WhatsApp adapter.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:WhatsApp:Twilio:AccountSid
///   Notify:WhatsApp:Twilio:AuthToken
///   Notify:WhatsApp:Twilio:FromNumber   (format: whatsapp:+14155238886)
///   Integration:ToWhatsApp              (format: +447700900000)
///
/// SETUP:
///   1. Twilio Console → Messaging → Try it out → Send a WhatsApp message
///   2. Text "join [sandbox-keyword]" from your personal number to the sandbox number
///   3. Your number is now enrolled — use it as ToWhatsApp
///   4. FromNumber: whatsapp:+14155238886  (Twilio sandbox number)
/// </summary>
public sealed class TwilioWhatsAppIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "whatsapp:twilio";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:WhatsApp:Twilio:AccountSid",
            "Notify:WhatsApp:Twilio:AuthToken",
            "Notify:WhatsApp:Twilio:FromNumber",
            "Integration:ToWhatsApp");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new TwilioOptions
        {
            AccountSid = config["Notify:WhatsApp:Twilio:AccountSid"]!,
            AuthToken = config["Notify:WhatsApp:Twilio:AuthToken"]!,
            FromNumber = config["Notify:WhatsApp:Twilio:FromNumber"]!
        };
        services.AddRecurPixelWhatsAppTwilio(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToWhatsApp"]!,
        Body = $"Your appointment is coming up on 12/1 at 3pm"
    };
}
