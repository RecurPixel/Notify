using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Email.Resend;
using RecurPixel.Notify.IntegrationTests.Infrastructure;

namespace RecurPixel.Notify.IntegrationTests.Email;

/// <summary>
/// Integration tests for the Resend email adapter.
/// Resend has no native batch API — base class loop handles bulk.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Email:Resend:ApiKey
///   Notify:Email:Resend:FromEmail
///   Integration:ToEmail
/// </summary>
public sealed class ResendIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "email:resend";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Email:Resend:ApiKey",
            "Notify:Email:Resend:FromEmail",
            "Integration:ToEmail");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new ResendOptions
        {
            ApiKey = config["Notify:Email:Resend:ApiKey"]!,
            FromEmail = config["Notify:Email:Resend:FromEmail"]!,
            FromName = config["Notify:Email:Resend:FromName"] ?? "RecurPixel Test"
        };
        services.AddResendChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToEmail"]!,
        Subject = $"RecurPixel.Notify Integration — Resend — {DateTime.UtcNow:u}",
        Body = "Integration test from RecurPixel.Notify. " +
                  "If you received this, the Resend adapter is working."
    };
}
