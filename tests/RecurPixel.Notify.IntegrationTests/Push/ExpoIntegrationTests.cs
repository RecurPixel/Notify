using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Push.Expo;

namespace RecurPixel.Notify.IntegrationTests.Push;

/// <summary>
/// Integration tests for the Expo Push adapter.
/// Expo supports native batch push — SendBulkAsync is overridden (UsedNativeBatch = true).
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Push:Expo:AccessToken   (optional — anonymous push works but is rate-limited)
///   Integration:ToDeviceToken      (format: ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx])
///
/// NOTE: AccessToken is optional for low-volume testing.
/// </summary>
public sealed class ExpoIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "push:expo";

    // Device token is the minimum — access token is optional
    protected override bool IsConfigured() =>
        TestConfiguration.Has("Integration:ToDeviceToken");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new ExpoOptions
        {
            AccessToken = config["Notify:Push:Expo:AccessToken"]
        };
        services.AddExpoChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToDeviceToken"]!,
        Subject = "RecurPixel.Notify Integration Test",
        Body = $"Expo push test — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
