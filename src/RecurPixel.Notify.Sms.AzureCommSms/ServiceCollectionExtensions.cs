using Azure.Communication.Sms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;
using RecurPixel.Notify.Sms.AzureCommSms;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Azure Communication Services SMS channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AzureCommSmsChannel"/> and its dependencies
    /// into the service collection.
    /// </summary>
    public static IServiceCollection AddAzureCommSmsChannel(
        this IServiceCollection services,
        AzureCommSmsOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddSingleton<IAzureCommSmsClient>(_ =>
            new AzureCommSmsClientWrapper(
                new SmsClient(options.ConnectionString)));

        services.AddSingleton<AzureCommSmsChannel>();

        services.TryAddKeyedSingleton<INotificationChannel, AzureCommSmsChannel>(
            "sms:azurecommsms");

        return services;
    }
}
