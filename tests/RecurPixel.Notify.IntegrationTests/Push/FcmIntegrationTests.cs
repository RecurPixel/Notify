using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Push.Fcm;

namespace RecurPixel.Notify.IntegrationTests.Push;

/// <summary>
/// Integration tests for the FCM (Firebase Cloud Messaging) push adapter.
/// FCM supports multicast — SendBulkAsync is overridden (UsedNativeBatch = true).
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Push:Fcm:ProjectId
///   Notify:Push:Fcm:ServiceAccountJson   (full JSON string — paste contents of the .json key file)
///   Integration:ToDeviceToken            (a real FCM device registration token from your app)
///
/// HOW TO GET:
///   1. console.firebase.google.com → your project → Project Settings
///   2. Service Accounts tab → Generate new private key → download JSON
///   3. Paste the entire JSON content as a single escaped string in ServiceAccountJson
///   4. Get a device token from your app: FirebaseMessaging.getInstance().getToken()
///
/// NOTE: The bulk test sends 3 messages to the same device token — valid for testing.
/// </summary>
public sealed class FcmIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "push:fcm";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Push:Fcm:ProjectId",
            "Notify:Push:Fcm:ServiceAccountJson",
            "Integration:ToDeviceToken");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new Core.Options.Providers.FcmOptions
        {
            ProjectId = config["Notify:Push:Fcm:ProjectId"]!,
            ServiceAccountJson = config["Notify:Push:Fcm:ServiceAccountJson"]!
        };
        services.AddRecurPixelFcm(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToDeviceToken"]!,
        Subject = "RecurPixel.Notify Integration Test",
        Body = $"FCM push test — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
