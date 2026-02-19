using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Sms.Vonage;

namespace RecurPixel.Notify.IntegrationTests.Sms;

/// <summary>
/// Integration tests for the Vonage SMS adapter.
/// Vonage supports native bulk SMS — SendBulkAsync is overridden (UsedNativeBatch = true).
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Sms:Vonage:ApiKey
///   Notify:Sms:Vonage:ApiSecret
///   Notify:Sms:Vonage:FromNumber
///   Integration:ToPhone
/// </summary>
public sealed class VonageSmsIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "sms:vonage";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Sms:Vonage:ApiKey",
            "Notify:Sms:Vonage:ApiSecret",
            "Notify:Sms:Vonage:FromNumber",
            "Integration:ToPhone");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new VonageOptions
        {
            ApiKey = config["Notify:Sms:Vonage:ApiKey"]!,
            ApiSecret = config["Notify:Sms:Vonage:ApiSecret"]!,
            FromNumber = config["Notify:Sms:Vonage:FromNumber"]!
        };
        services.AddVonageSmsChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToPhone"]!,
        Body = $"RecurPixel.Notify integration test — Vonage SMS — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
