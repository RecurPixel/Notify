using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Viber Business Messages notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.ViberChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="ViberRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddViberChannel(
        this IServiceCollection services,
        ViberOptions options)
    {
        new ViberRegistrar().Register(services,
            new NotifyOptions { Viber = options });
        return services;
    }
}
