using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Email.Postmark;
using RecurPixel.Notify.IntegrationTests.Infrastructure;

namespace RecurPixel.Notify.IntegrationTests.Email;

/// <summary>
/// Integration tests for the Postmark email adapter.
/// Postmark supports native batch sending — SendBulkAsync is overridden (UsedNativeBatch = true).
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Email:Postmark:ApiKey
///   Notify:Email:Postmark:FromEmail
///   Integration:ToEmail
/// </summary>
public sealed class PostmarkIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "email:postmark";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Email:Postmark:ApiKey",
            "Notify:Email:Postmark:FromEmail",
            "Integration:ToEmail");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new PostmarkOptions
        {
            ApiKey = config["Notify:Email:Postmark:ApiKey"]!,
            FromEmail = config["Notify:Email:Postmark:FromEmail"]!,
            FromName = config["Notify:Email:Postmark:FromName"] ?? "RecurPixel Test"
        };
        services.AddPostmarkChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToEmail"]!,
        Subject = $"RecurPixel.Notify Integration — Postmark — {DateTime.UtcNow:u}",
        Body = "Integration test from RecurPixel.Notify. " +
                  "If you received this, the Postmark adapter is working."
    };
}
