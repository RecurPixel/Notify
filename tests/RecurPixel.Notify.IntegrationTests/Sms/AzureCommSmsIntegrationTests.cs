using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Sms.AzureCommSms;

namespace RecurPixel.Notify.IntegrationTests.Sms;

/// <summary>
/// Integration tests for the Azure Communication Services SMS adapter (Phase 12B).
/// ACS SMS supports native batch sending — SendBulkAsync is overridden (UsedNativeBatch = true).
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Sms:AzureCommSms:ConnectionString
///   Notify:Sms:AzureCommSms:FromNumber   (format: +12345678900 — provisioned ACS phone number)
///   Integration:ToPhone                  (format: +447700900000)
///
/// HOW TO GET:
///   1. Azure Portal → Communication Services resource (same resource as ACS Email)
///   2. Resource → Phone Numbers → Get a number
///   3. Resource → Keys → Copy Connection String
///   4. Use the provisioned number as FromNumber
/// </summary>
public sealed class AzureCommSmsIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "sms:azurecommsms";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Sms:AzureCommSms:ConnectionString",
            "Notify:Sms:AzureCommSms:FromNumber",
            "Integration:ToPhone");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new AzureCommSmsOptions
        {
            ConnectionString = config["Notify:Sms:AzureCommSms:ConnectionString"]!,
            FromNumber = config["Notify:Sms:AzureCommSms:FromNumber"]!
        };
        services.AddAzureCommSmsChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToPhone"]!,
        Body = $"RecurPixel.Notify integration test — Azure Comm SMS — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
