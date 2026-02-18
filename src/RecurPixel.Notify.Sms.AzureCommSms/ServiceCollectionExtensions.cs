using Azure.Communication.Sms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;

namespace RecurPixel.Notify.Sms.AzureCommSms;

/// <summary>
/// DI registration extensions for the Azure Communication Services SMS channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AzureCommSmsChannel"/> and its dependencies
    /// into the service collection.
    /// </summary>
    internal static IServiceCollection AddAzureCommSmsChannel(
        this IServiceCollection services,
        AzureCommSmsOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddSingleton<IAzureCommSmsClient>(_ =>
            new AzureCommSmsClientWrapper(
                new SmsClient(options.ConnectionString)));

        services.AddSingleton<AzureCommSmsChannel>();

        services.AddKeyedSingleton<INotificationChannel, AzureCommSmsChannel>(
            "sms:azurecommsms");

        return services;
    }
}
