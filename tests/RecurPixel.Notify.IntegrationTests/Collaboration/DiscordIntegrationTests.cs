using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Discord;
using RecurPixel.Notify.IntegrationTests.Infrastructure;

namespace RecurPixel.Notify.IntegrationTests.Collaboration;

/// <summary>
/// Integration tests for the Discord webhook adapter.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Discord:WebhookUrl
///
/// SETUP (1 minute):
///   1. Open Discord → right-click any channel you own → Edit Channel
///   2. Integrations → Webhooks → New Webhook
///   3. Give it a name → Copy Webhook URL → paste in config
/// </summary>
public sealed class DiscordIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "discord:discord";

    protected override bool IsConfigured() =>
        TestConfiguration.Has("Notify:Discord:WebhookUrl");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new DiscordOptions
        {
            WebhookUrl = config["Notify:Discord:WebhookUrl"]!
        };
        services.AddDiscordChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Notify:Discord:WebhookUrl"]!,
        Body = $"✅ **RecurPixel.Notify** integration test — Discord — `{DateTime.UtcNow:HH:mm:ss} UTC`"
    };
}
