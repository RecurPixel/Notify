using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Email.Smtp;
using RecurPixel.Notify.IntegrationTests.Infrastructure;

namespace RecurPixel.Notify.IntegrationTests.Email;

/// <summary>
/// Integration tests for the SMTP email adapter.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Email:Smtp:Host
///   Notify:Email:Smtp:Username
///   Notify:Email:Smtp:Password
///   Notify:Email:Smtp:FromEmail
///   Integration:ToEmail
///
/// GMAIL SETUP:
///   Host: smtp.gmail.com  Port: 587  UseSsl: true
///   Google Account → Security → 2-Step Verification → App Passwords → Generate
///   Use the generated 16-char password as Password.
/// </summary>
public sealed class SmtpIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "email:smtp";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Email:Smtp:Host",
            "Notify:Email:Smtp:Username",
            "Notify:Email:Smtp:Password",
            "Notify:Email:Smtp:FromEmail",
            "Integration:ToEmail");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new SmtpOptions
        {
            Host = config["Notify:Email:Smtp:Host"]!,
            Port = int.TryParse(config["Notify:Email:Smtp:Port"], out var p) ? p : 587,
            Username = config["Notify:Email:Smtp:Username"]!,
            Password = config["Notify:Email:Smtp:Password"]!,
            UseSsl = !bool.TryParse(config["Notify:Email:Smtp:UseSsl"], out var ssl) || ssl,
            FromEmail = config["Notify:Email:Smtp:FromEmail"]!,
            FromName = config["Notify:Email:Smtp:FromName"] ?? "RecurPixel Test"
        };
        services.AddSmtpChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToEmail"]!,
        Subject = $"RecurPixel.Notify Integration — SMTP — {DateTime.UtcNow:u}",
        Body = "Integration test from RecurPixel.Notify. " +
                  "If you received this, the SMTP adapter is working."
    };
}
