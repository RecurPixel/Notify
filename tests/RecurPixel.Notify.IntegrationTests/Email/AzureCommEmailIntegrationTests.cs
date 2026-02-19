using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Email.AzureCommEmail;
using RecurPixel.Notify.IntegrationTests.Infrastructure;

namespace RecurPixel.Notify.IntegrationTests.Email;

/// <summary>
/// Integration tests for the Azure Communication Services Email adapter (Phase 12B).
/// ACS Email has no native batch API — base class loop handles bulk.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Email:AzureCommEmail:ConnectionString
///   Notify:Email:AzureCommEmail:FromEmail
///   Integration:ToEmail
///
/// HOW TO GET:
///   1. Azure Portal → Create Resource → Communication Services
///   2. Resource → Keys → Copy Connection String
///   3. Resource → Email → Domains → Add a domain (or use the free Azure subdomain)
///   4. Use the provisioned sender address as FromEmail
/// </summary>
public sealed class AzureCommEmailIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "email:azurecommemail";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Email:AzureCommEmail:ConnectionString",
            "Notify:Email:AzureCommEmail:FromEmail",
            "Integration:ToEmail");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new AzureCommEmailOptions
        {
            ConnectionString = config["Notify:Email:AzureCommEmail:ConnectionString"]!,
            FromEmail = config["Notify:Email:AzureCommEmail:FromEmail"]!,
            FromName = config["Notify:Email:AzureCommEmail:FromName"] ?? "RecurPixel Test"
        };
        services.AddAzureCommEmailChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToEmail"]!,
        Subject = $"RecurPixel.Notify Integration — Azure Comm Email — {DateTime.UtcNow:u}",
        Body = "<p>Integration test from <strong>RecurPixel.Notify</strong>. " +
                  "If you received this, the Azure Communication Services Email adapter is working.</p>"
    };
}
