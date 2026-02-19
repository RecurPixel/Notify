using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.WhatsApp.Vonage;

/// <summary>
/// DI registration extensions for the Vonage WhatsApp channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="VonageWhatsAppChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The Vonage options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddVonageWhatsAppChannel(
        this IServiceCollection services,
        VonageWhatsAppOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<VonageWhatsAppChannel>();

        services.AddKeyedSingleton<INotificationChannel, VonageWhatsAppChannel>("whatsapp:vonage");

        return services;
    }
}
