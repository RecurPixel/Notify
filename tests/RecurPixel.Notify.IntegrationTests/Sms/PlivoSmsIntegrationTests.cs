using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Sms.Plivo;

namespace RecurPixel.Notify.IntegrationTests.Sms;

/// <summary>
/// Integration tests for the Plivo SMS adapter.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Sms:Plivo:AuthId
///   Notify:Sms:Plivo:AuthToken
///   Notify:Sms:Plivo:FromNumber
///   Integration:ToPhone
/// </summary>
public sealed class PlivoSmsIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "sms:plivo";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Sms:Plivo:AuthId",
            "Notify:Sms:Plivo:AuthToken",
            "Notify:Sms:Plivo:FromNumber",
            "Integration:ToPhone");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new PlivoOptions
        {
            AuthId = config["Notify:Sms:Plivo:AuthId"]!,
            AuthToken = config["Notify:Sms:Plivo:AuthToken"]!,
            FromNumber = config["Notify:Sms:Plivo:FromNumber"]!
        };
        services.AddPlivoChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToPhone"]!,
        Body = $"RecurPixel.Notify integration test — Plivo SMS — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
