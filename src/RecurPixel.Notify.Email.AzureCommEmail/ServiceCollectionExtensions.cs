using Azure.Communication.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;

namespace RecurPixel.Notify.Email.AzureCommEmail;

/// <summary>
/// DI registration extensions for the Azure Communication Services Email channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AzureCommEmailChannel"/> and its dependencies
    /// into the service collection.
    /// </summary>
    internal static IServiceCollection AddAzureCommEmailChannel(
        this IServiceCollection services,
        AzureCommEmailOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddSingleton<IAzureCommEmailClient>(_ =>
            new AzureCommEmailClientWrapper(
                new EmailClient(options.ConnectionString)));

        services.AddSingleton<AzureCommEmailChannel>();

        services.AddKeyedSingleton<INotificationChannel, AzureCommEmailChannel>(
            "email:azurecommemail");

        return services;
    }
}
