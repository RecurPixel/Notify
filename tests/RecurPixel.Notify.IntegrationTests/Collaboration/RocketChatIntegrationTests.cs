using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.RocketChat;

namespace RecurPixel.Notify.IntegrationTests.Collaboration;

/// <summary>
/// Integration tests for the Rocket.Chat webhook adapter (Phase 12B).
/// Rocket.Chat has no native bulk API — base class loop handles bulk.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:RocketChat:WebhookUrl
///
/// HOW TO GET:
///   Self-hosted:  Rocket.Chat → Administration → Integrations → New → Incoming
///   Cloud:        open.rocket.chat → register → same path
///   The webhook URL is shown after saving the integration.
/// </summary>
public sealed class RocketChatIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "rocketchat:rocketchat";

    protected override bool IsConfigured() =>
        TestConfiguration.Has("Notify:RocketChat:WebhookUrl");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new RocketChatOptions
        {
            WebhookUrl = config["Notify:RocketChat:WebhookUrl"]!
        };
        services.AddRocketChatChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Notify:RocketChat:WebhookUrl"]!,
        Body = $"RecurPixel.Notify integration test — Rocket.Chat — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
