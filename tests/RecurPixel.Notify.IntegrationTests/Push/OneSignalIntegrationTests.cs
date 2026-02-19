using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Push.OneSignal;

namespace RecurPixel.Notify.IntegrationTests.Push;

/// <summary>
/// Integration tests for the OneSignal push adapter.
/// OneSignal supports native bulk notifications — SendBulkAsync is overridden (UsedNativeBatch = true).
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Push:OneSignal:AppId
///   Notify:Push:OneSignal:ApiKey
///   Integration:ToDeviceToken
/// </summary>
public sealed class OneSignalIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "push:onesignal";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Push:OneSignal:AppId",
            "Notify:Push:OneSignal:ApiKey",
            "Integration:ToDeviceToken");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new OneSignalOptions
        {
            AppId = config["Notify:Push:OneSignal:AppId"]!,
            ApiKey = config["Notify:Push:OneSignal:ApiKey"]!
        };
        services.AddOneSignalChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToDeviceToken"]!,
        Subject = "RecurPixel.Notify Integration Test",
        Body = $"OneSignal push test — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
