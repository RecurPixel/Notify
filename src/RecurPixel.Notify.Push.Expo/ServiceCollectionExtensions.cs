using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Expo push notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.ExpoChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="ExpoRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddExpoChannel(
        this IServiceCollection services,
        ExpoOptions options)
    {
        new ExpoRegistrar().Register(services,
            new NotifyOptions { Push = new PushOptions { Expo = options } });
        return services;
    }
}
