using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.WhatsApp.MetaCloud;

namespace RecurPixel.Notify.IntegrationTests.WhatsApp;

/// <summary>
/// Integration tests for the Meta Cloud WhatsApp adapter.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:WhatsApp:MetaCloud:PhoneNumberId   (numeric ID from Meta Developer portal)
///   Notify:WhatsApp:MetaCloud:AccessToken     (temporary or permanent access token)
///   Integration:ToWhatsApp                    (format: +447700900000)
///
/// SETUP:
///   1. developers.facebook.com → My Apps → Create App → Business
///   2. Add WhatsApp product → Getting Started
///   3. PhoneNumberId: shown under the "From" phone number
///   4. AccessToken: shown as "Temporary access token" (valid 24h)
///   5. Add recipient number as a test number in the Meta portal
/// </summary>
public sealed class MetaCloudWhatsAppIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "whatsapp:metacloud";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:WhatsApp:MetaCloud:PhoneNumberId",
            "Notify:WhatsApp:MetaCloud:AccessToken",
            "Integration:ToWhatsApp");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new MetaCloudOptions
        {
            PhoneNumberId = config["Notify:WhatsApp:MetaCloud:PhoneNumberId"]!,
            AccessToken = config["Notify:WhatsApp:MetaCloud:AccessToken"]!
        };
        services.AddRecurPixelWhatsAppMetaCloud(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToWhatsApp"]!,
        Body = $"RecurPixel.Notify integration test — Meta Cloud WhatsApp — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
