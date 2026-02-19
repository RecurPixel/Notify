using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Email.Mailgun;
using RecurPixel.Notify.IntegrationTests.Infrastructure;

namespace RecurPixel.Notify.IntegrationTests.Email;

/// <summary>
/// Integration tests for the Mailgun email adapter.
/// Mailgun supports native batch sending — SendBulkAsync is overridden (UsedNativeBatch = true).
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Email:Mailgun:ApiKey
///   Notify:Email:Mailgun:Domain
///   Notify:Email:Mailgun:FromEmail
///   Integration:ToEmail
/// </summary>
public sealed class MailgunIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "email:mailgun";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Email:Mailgun:ApiKey",
            "Notify:Email:Mailgun:Domain",
            "Notify:Email:Mailgun:FromEmail",
            "Integration:ToEmail");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new MailgunOptions
        {
            ApiKey = config["Notify:Email:Mailgun:ApiKey"]!,
            Domain = config["Notify:Email:Mailgun:Domain"]!,
            FromEmail = config["Notify:Email:Mailgun:FromEmail"]!,
            FromName = config["Notify:Email:Mailgun:FromName"] ?? "RecurPixel Test"
        };
        services.AddMailgunChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToEmail"]!,
        Subject = $"RecurPixel.Notify Integration — Mailgun — {DateTime.UtcNow:u}",
        Body = "Integration test from RecurPixel.Notify. " +
                  "If you received this, the Mailgun adapter is working."
    };
}
