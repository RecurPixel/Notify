using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Azure Communication Services SMS channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.AzureCommSmsChannel"/> and its <see cref="Sms.AzureCommSms.IAzureCommSmsClient"/>.
    /// Delegates to <see cref="AzureCommSmsRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddAzureCommSmsChannel(
        this IServiceCollection services,
        AzureCommSmsOptions options)
    {
        new AzureCommSmsRegistrar().Register(services,
            new NotifyOptions { Sms = new SmsOptions { AzureCommSms = options } });
        return services;
    }
}
