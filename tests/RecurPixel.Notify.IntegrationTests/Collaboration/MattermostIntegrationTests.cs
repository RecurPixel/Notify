using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Mattermost;

namespace RecurPixel.Notify.IntegrationTests.Collaboration;

/// <summary>
/// Integration tests for the Mattermost webhook adapter (Phase 12B).
/// Mattermost has no native bulk API — base class loop handles bulk.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Mattermost:WebhookUrl
///
/// HOW TO GET:
///   Self-hosted:  Mattermost → Main Menu → Integrations → Incoming Webhooks → Add
///   Cloud:        cloud.mattermost.com → same path
///   Free team:    mattermost.com/sign-up → create a workspace
/// </summary>
public sealed class MattermostIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "mattermost:mattermost";

    protected override bool IsConfigured() =>
        TestConfiguration.Has("Notify:Mattermost:WebhookUrl");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new MattermostOptions
        {
            WebhookUrl = config["Notify:Mattermost:WebhookUrl"]!
        };
        services.AddMattermostChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Notify:Mattermost:WebhookUrl"]!,
        Body = $"RecurPixel.Notify integration test — Mattermost — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
