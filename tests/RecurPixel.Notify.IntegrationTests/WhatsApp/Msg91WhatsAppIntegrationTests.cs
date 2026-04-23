using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;
using RecurPixel.Notify.IntegrationTests.Infrastructure;

namespace RecurPixel.Notify.IntegrationTests.WhatsApp;

/// <summary>
/// Integration tests for the MSG91 WhatsApp adapter.
/// MSG91 has no native WhatsApp bulk API — base class loop handles bulk automatically.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:WhatsApp:Msg91:AuthKey           (from the MSG91 portal)
///   Notify:WhatsApp:Msg91:IntegratedNumber  (MSG91 integrated WhatsApp number, e.g. "919XXXXXXXXX")
///   Integration:ToWhatsApp                  (format: +919999999999)
///
/// SETUP:
///   1. Log in to MSG91 → WhatsApp → Integrated Numbers
///   2. Note the integrated number (without + prefix)
///   3. Ensure the destination number has an active WhatsApp account
/// </summary>
public sealed class Msg91WhatsAppIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "whatsapp:msg91";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:WhatsApp:Msg91:AuthKey",
            "Notify:WhatsApp:Msg91:IntegratedNumber",
            "Integration:ToWhatsApp");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new Msg91WhatsAppOptions
        {
            AuthKey          = config["Notify:WhatsApp:Msg91:AuthKey"]!,
            IntegratedNumber = config["Notify:WhatsApp:Msg91:IntegratedNumber"]!
        };
        services.AddMsg91WhatsAppChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To   = config["Integration:ToWhatsApp"]!,
        Body = $"RecurPixel.Notify integration test — MSG91 WhatsApp — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
