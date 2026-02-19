using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Viber;

namespace RecurPixel.Notify.IntegrationTests.Social;

/// <summary>
/// Integration tests for the Viber Business Messages adapter.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Viber:BotAuthToken   (from Viber Admin Panel)
///   Notify:Viber:SenderName     (your bot display name, max 28 characters)
///   Integration:ToViberUserId   (Viber user ID)
///
/// HOW TO GET:
///   1. partners.viber.com → Create a Bot Account
///   2. After creation you receive the auth token
///   3. User ID is returned when a user sends a message to your bot
/// </summary>
public sealed class ViberIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "viber:viber";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Viber:BotAuthToken",
            "Integration:ToViberUserId");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new ViberOptions
        {
            BotAuthToken = config["Notify:Viber:BotAuthToken"]!,
            SenderName = config["Notify:Viber:SenderName"] ?? "RecurPixel Test"
        };
        services.AddViberChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToViberUserId"]!,
        Body = $"RecurPixel.Notify integration test — Viber — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
