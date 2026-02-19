using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.WhatsApp.Vonage;

namespace RecurPixel.Notify.IntegrationTests.WhatsApp;

/// <summary>
/// Integration tests for the Vonage WhatsApp adapter.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:WhatsApp:Vonage:ApiKey
///   Notify:WhatsApp:Vonage:ApiSecret
///   Notify:WhatsApp:Vonage:FromNumber
///   Integration:ToWhatsApp
/// </summary>
public sealed class VonageWhatsAppIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "whatsapp:vonage";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:WhatsApp:Vonage:ApiKey",
            "Notify:WhatsApp:Vonage:ApiSecret",
            "Notify:WhatsApp:Vonage:FromNumber",
            "Integration:ToWhatsApp");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new VonageWhatsAppOptions
        {
            ApiKey = config["Notify:WhatsApp:Vonage:ApiKey"]!,
            ApiSecret = config["Notify:WhatsApp:Vonage:ApiSecret"]!,
            FromNumber = config["Notify:WhatsApp:Vonage:FromNumber"]!
        };
        services.AddVonageWhatsAppChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToWhatsApp"]!,
        Body = $"RecurPixel.Notify integration test — Vonage WhatsApp — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
