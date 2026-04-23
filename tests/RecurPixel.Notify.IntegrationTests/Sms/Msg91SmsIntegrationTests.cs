using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;
using RecurPixel.Notify.IntegrationTests.Infrastructure;

namespace RecurPixel.Notify.IntegrationTests.Sms;

/// <summary>
/// Integration tests for the MSG91 SMS adapter.
/// MSG91 has no native bulk SMS API — base class loop handles bulk automatically.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Sms:Msg91:AuthKey    (from the MSG91 portal)
///   Notify:Sms:Msg91:SenderId   (up to 6-character sender ID registered with MSG91)
///   Integration:ToPhone         (format: +919999999999)
///
/// NOTE: MSG91 requires the destination number to have opted in on transactional routes.
///       Use a verified number for testing.
/// </summary>
public sealed class Msg91SmsIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "sms:msg91";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Sms:Msg91:AuthKey",
            "Notify:Sms:Msg91:SenderId",
            "Integration:ToPhone");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new Msg91SmsOptions
        {
            AuthKey  = config["Notify:Sms:Msg91:AuthKey"]!,
            SenderId = config["Notify:Sms:Msg91:SenderId"]!
        };
        services.AddMsg91SmsChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To   = config["Integration:ToPhone"]!,
        Body = $"RecurPixel.Notify integration test — MSG91 SMS — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
