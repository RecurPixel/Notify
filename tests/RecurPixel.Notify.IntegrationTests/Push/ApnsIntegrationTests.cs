using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Push.Apns;

namespace RecurPixel.Notify.IntegrationTests.Push;

/// <summary>
/// Integration tests for the APNs (Apple Push Notification service) adapter.
/// APNs has no bulk API — base class loop handles bulk (UsedNativeBatch = false).
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Push:Apns:KeyId        (10-character Key ID from Apple Developer)
///   Notify:Push:Apns:TeamId       (10-character Team ID from Apple Developer)
///   Notify:Push:Apns:BundleId     (your app bundle ID, e.g. com.example.myapp)
///   Notify:Push:Apns:PrivateKey   (full contents of the .p8 file including header/footer)
///   Integration:ToDeviceToken     (APNs device token — 64 hex characters)
///
/// HOW TO GET:
///   1. developer.apple.com → Certificates, IDs and Profiles → Keys
///   2. Create a key with Apple Push Notifications capability
///   3. Download the .p8 file — paste its entire content as PrivateKey
///   4. Team ID: top-right of developer.apple.com
///   5. Key ID: shown when you view the key
///
/// REQUIRES: Paid Apple Developer account ($99/yr). Skip if unavailable.
/// </summary>
public sealed class ApnsIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "push:apns";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Push:Apns:KeyId",
            "Notify:Push:Apns:TeamId",
            "Notify:Push:Apns:BundleId",
            "Notify:Push:Apns:PrivateKey",
            "Integration:ToDeviceToken");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new ApnsOptions
        {
            KeyId = config["Notify:Push:Apns:KeyId"]!,
            TeamId = config["Notify:Push:Apns:TeamId"]!,
            BundleId = config["Notify:Push:Apns:BundleId"]!,
            PrivateKey = config["Notify:Push:Apns:PrivateKey"]!
        };
        services.AddRecurPixelApns(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToDeviceToken"]!,
        Subject = "RecurPixel.Notify Integration Test",
        Body = $"APNs push test — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
