using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Teams;

namespace RecurPixel.Notify.IntegrationTests.Collaboration;

/// <summary>
/// Integration tests for the Microsoft Teams webhook adapter.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Teams:WebhookUrl
///
/// SETUP:
///   1. Open Teams → go to any channel → click ··· → Manage Channel
///   2. Connectors → Incoming Webhook → Add → Configure
///   3. Give it a name → Create → Copy the webhook URL → paste in config
///
/// FREE TEAMS ACCOUNT: developer.microsoft.com/en-us/microsoft-365/dev-program
/// </summary>
public sealed class TeamsIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "teams:teams";

    protected override bool IsConfigured() =>
        TestConfiguration.Has("Notify:Teams:WebhookUrl");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new TeamsOptions
        {
            WebhookUrl = config["Notify:Teams:WebhookUrl"]!
        };
        services.AddTeamsChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Notify:Teams:WebhookUrl"]!,
        Subject = "RecurPixel.Notify Integration Test",
        Body = $"RecurPixel.Notify integration test — Microsoft Teams — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
