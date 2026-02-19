using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Sms.MessageBird;

namespace RecurPixel.Notify.IntegrationTests.Sms;

/// <summary>
/// Integration tests for the MessageBird SMS adapter.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Sms:MessageBird:AccessKey
///   Notify:Sms:MessageBird:Originator
///   Integration:ToPhone
/// </summary>
public sealed class MessageBirdSmsIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "sms:messagebird";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Sms:MessageBird:AccessKey",
            "Notify:Sms:MessageBird:Originator",
            "Integration:ToPhone");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new MessageBirdOptions
        {
            ApiKey = config["Notify:Sms:MessageBird:AccessKey"]!,
            Originator = config["Notify:Sms:MessageBird:Originator"]!
        };
        services.AddMessageBirdChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToPhone"]!,
        Body = $"RecurPixel.Notify integration test — MessageBird SMS — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
