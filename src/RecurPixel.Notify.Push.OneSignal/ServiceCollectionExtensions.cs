using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the OneSignal push notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.OneSignalChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="OneSignalRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddOneSignalChannel(
        this IServiceCollection services,
        OneSignalOptions options)
    {
        new OneSignalRegistrar().Register(services,
            new NotifyOptions { Push = new PushOptions { OneSignal = options } });
        return services;
    }
}
