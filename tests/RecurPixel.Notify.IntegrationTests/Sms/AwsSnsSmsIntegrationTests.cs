using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Sms.AwsSns;

namespace RecurPixel.Notify.IntegrationTests.Sms;

/// <summary>
/// Integration tests for the AWS SNS SMS adapter.
/// AWS SNS supports topic publish for bulk — SendBulkAsync is overridden (UsedNativeBatch = true).
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Sms:AwsSns:AccessKey
///   Notify:Sms:AwsSns:SecretKey
///   Notify:Sms:AwsSns:Region
///   Integration:ToPhone
/// </summary>
public sealed class AwsSnsSmsIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "sms:awssns";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Sms:AwsSns:AccessKey",
            "Notify:Sms:AwsSns:SecretKey",
            "Notify:Sms:AwsSns:Region",
            "Integration:ToPhone");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new AwsSnsOptions
        {
            AccessKey = config["Notify:Sms:AwsSns:AccessKey"]!,
            SecretKey = config["Notify:Sms:AwsSns:SecretKey"]!,
            Region = config["Notify:Sms:AwsSns:Region"]!
        };
        services.AddAwsSnsChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Integration:ToPhone"]!,
        Body = $"RecurPixel.Notify integration test — AWS SNS SMS — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
