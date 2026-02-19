using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Email.AwsSes;
using RecurPixel.Notify.IntegrationTests.Infrastructure;

namespace RecurPixel.Notify.IntegrationTests.Email;

/// <summary>
/// Integration tests for the AWS SES email adapter.
/// AWS SES supports native batch sending — SendBulkAsync is overridden (UsedNativeBatch = true).
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Email:AwsSes:AccessKey
///   Notify:Email:AwsSes:SecretKey
///   Notify:Email:AwsSes:Region
///   Notify:Email:AwsSes:FromEmail
///   Integration:ToEmail
///
/// NOTE: FromEmail must be verified in AWS SES before use.
/// </summary>
public sealed class AwsSesIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "email:awsses";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Email:AwsSes:AccessKey",
            "Notify:Email:AwsSes:SecretKey",
            "Notify:Email:AwsSes:Region",
            "Notify:Email:AwsSes:FromEmail",
            "Integration:ToEmail");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new AwsSesOptions
        {
            AccessKey = config["Notify:Email:AwsSes:AccessKey"]!,
            SecretKey = config["Notify:Email:AwsSes:SecretKey"]!,
            Region = config["Notify:Email:AwsSes:Region"]!,
            FromEmail = config["Notify:Email:AwsSes:FromEmail"]!,
            FromName = config["Notify:Email:AwsSes:FromName"] ?? "RecurPixel Test"
        };
        services.AddAwsSesChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToEmail"]!,
        Subject = $"RecurPixel.Notify Integration — AWS SES — {DateTime.UtcNow:u}",
        Body = "Integration test from RecurPixel.Notify. " +
                  "If you received this, the AWS SES adapter is working."
    };
}
