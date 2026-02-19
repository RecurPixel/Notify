using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Email.SendGrid;
using RecurPixel.Notify.IntegrationTests.Infrastructure;

namespace RecurPixel.Notify.IntegrationTests.Email;

/// <summary>
/// Integration tests for the SendGrid email adapter.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Email:SendGrid:ApiKey
///   Notify:Email:SendGrid:FromEmail
///   Integration:ToEmail
///
/// HOW TO GET:
///   sendgrid.com → Settings → API Keys → Create API Key (Mail Send permission only)
/// </summary>
public sealed class SendGridIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "email:sendgrid";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Email:SendGrid:ApiKey",
            "Notify:Email:SendGrid:FromEmail",
            "Integration:ToEmail");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new SendGridOptions
        {
            ApiKey = config["Notify:Email:SendGrid:ApiKey"]!,
            FromEmail = config["Notify:Email:SendGrid:FromEmail"]!,
            FromName = config["Notify:Email:SendGrid:FromName"] ?? "RecurPixel Test"
        };
        services.AddSendGridChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToEmail"]!,
        Subject = $"RecurPixel.Notify Integration — SendGrid — {DateTime.UtcNow:u}",
        Body = "<p>Integration test from <strong>RecurPixel.Notify</strong>. " +
                  "If you received this, the SendGrid adapter is working.</p>"
    };
}
