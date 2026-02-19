using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Push.Expo;

/// <summary>
/// DI registration extensions for the Expo push notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ExpoChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The Expo options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddExpoChannel(
        this IServiceCollection services,
        ExpoOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<ExpoChannel>();

        services.AddKeyedSingleton<INotificationChannel, ExpoChannel>("push:expo");

        return services;
    }
}
