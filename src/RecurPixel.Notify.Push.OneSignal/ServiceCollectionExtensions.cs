using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the OneSignal push notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OneSignalChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The OneSignal options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddOneSignalChannel(
        this IServiceCollection services,
        OneSignalOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<OneSignalChannel>();

        services.TryAddKeyedSingleton<INotificationChannel, OneSignalChannel>("push:onesignal");

        return services;
    }
}
