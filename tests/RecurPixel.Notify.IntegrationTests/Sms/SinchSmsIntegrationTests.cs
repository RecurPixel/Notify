using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Sms.Sinch;

namespace RecurPixel.Notify.IntegrationTests.Sms;

/// <summary>
/// Integration tests for the Sinch SMS adapter.
/// Sinch supports native bulk SMS — SendBulkAsync is overridden (UsedNativeBatch = true).
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Sms:Sinch:ApiToken
///   Notify:Sms:Sinch:ServicePlanId
///   Notify:Sms:Sinch:FromNumber
///   Integration:ToPhone
/// </summary>
public sealed class SinchSmsIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "sms:sinch";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Sms:Sinch:ApiToken",
            "Notify:Sms:Sinch:ServicePlanId",
            "Notify:Sms:Sinch:FromNumber",
            "Integration:ToPhone");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new SinchOptions
        {
            ApiToken = config["Notify:Sms:Sinch:ApiToken"]!,
            ServicePlanId = config["Notify:Sms:Sinch:ServicePlanId"]!,
            FromNumber = config["Notify:Sms:Sinch:FromNumber"]!
        };
        services.AddSinchChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToPhone"]!,
        Body = $"RecurPixel.Notify integration test — Sinch SMS — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
