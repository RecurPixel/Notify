using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the MSG91 WhatsApp channel.
/// </summary>
public static class Msg91WhatsAppServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Msg91WhatsAppChannel"/> and its named <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">MSG91 WhatsApp options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddMsg91WhatsAppChannel(
        this IServiceCollection services,
        Msg91WhatsAppOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient("whatsapp:msg91", http =>
        {
            http.Timeout = TimeSpan.FromSeconds(30);
        });

        services.TryAddKeyedSingleton<INotificationChannel, Msg91WhatsAppChannel>("whatsapp:msg91");

        return services;
    }
}
