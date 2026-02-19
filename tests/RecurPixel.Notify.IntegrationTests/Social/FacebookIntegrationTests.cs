using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Facebook;
using RecurPixel.Notify.IntegrationTests.Infrastructure;

namespace RecurPixel.Notify.IntegrationTests.Social;

/// <summary>
/// Integration tests for the Facebook Messenger adapter (Phase 10).
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Facebook:PageAccessToken   (from Facebook Developer portal)
///   Integration:ToFacebookRecipientId (PSID — Page-Scoped ID of the recipient)
///
/// HOW TO GET:
///   1. developers.facebook.com → My Apps → Create App → Business
///   2. Add Messenger product → Settings → Generate Page Access Token
///   3. Recipient must have messaged your page first to get their PSID
/// </summary>
public sealed class FacebookIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "facebook:facebook";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Facebook:PageAccessToken",
            "Integration:ToFacebookRecipientId");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new FacebookOptions
        {
            PageAccessToken = config["Notify:Facebook:PageAccessToken"]!
        };
        services.AddFacebookChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToFacebookRecipientId"]!,
        Body = $"RecurPixel.Notify integration test — Facebook Messenger — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
