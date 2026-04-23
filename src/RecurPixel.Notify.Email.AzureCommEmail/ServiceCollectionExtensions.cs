using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Azure Communication Services Email channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.AzureCommEmailChannel"/> and its <see cref="Email.AzureCommEmail.IAzureCommEmailClient"/>.
    /// Delegates to <see cref="AzureCommEmailRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddAzureCommEmailChannel(
        this IServiceCollection services,
        AzureCommEmailOptions options)
    {
        new AzureCommEmailRegistrar().Register(services,
            new NotifyOptions { Email = new EmailOptions { AzureCommEmail = options } });
        return services;
    }
}
