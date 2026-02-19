using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Slack;

namespace RecurPixel.Notify.IntegrationTests.Collaboration;

/// <summary>
/// Integration tests for the Slack webhook adapter.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Slack:WebhookUrl
///
/// SETUP (2 minutes):
///   1. api.slack.com/apps → Create New App → From scratch
///   2. Incoming Webhooks → Activate Incoming Webhooks → On
///   3. Add New Webhook to Workspace → choose channel → Allow
///   4. Copy the webhook URL → paste in config
/// </summary>
public sealed class SlackIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "slack:slack";

    protected override bool IsConfigured() =>
        TestConfiguration.Has("Notify:Slack:WebhookUrl");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new SlackOptions
        {
            WebhookUrl = config["Notify:Slack:WebhookUrl"]!
        };
        services.AddSlackChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Notify:Slack:WebhookUrl"]!,
        Subject = "RecurPixel.Notify Integration Test",
        Body = $":white_check_mark: *RecurPixel.Notify* integration test — Slack — `{DateTime.UtcNow:HH:mm:ss} UTC`"
    };
}
