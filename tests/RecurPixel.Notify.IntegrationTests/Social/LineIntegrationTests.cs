using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Line;

namespace RecurPixel.Notify.IntegrationTests.Social;

/// <summary>
/// Integration tests for the LINE Messaging API adapter (Phase 10).
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Line:ChannelAccessToken   (from LINE Developers console)
///   Integration:ToLineUserId         (LINE user ID — starts with "U", 33 characters)
///
/// HOW TO GET:
///   1. developers.line.biz → Create a Provider → Create a Messaging API channel
///   2. Channel settings → Messaging API → Channel access token → Issue
///   3. User ID: add your bot as a friend, then use the webhook or getProfile API
/// </summary>
public sealed class LineIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "line:line";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Line:ChannelAccessToken",
            "Integration:ToLineUserId");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new LineOptions
        {
            ChannelAccessToken = config["Notify:Line:ChannelAccessToken"]!
        };
        services.AddLineChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToLineUserId"]!,
        Body = $"RecurPixel.Notify integration test — LINE — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
