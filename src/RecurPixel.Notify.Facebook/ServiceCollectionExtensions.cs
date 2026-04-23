using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Facebook Messenger notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.FacebookChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="FacebookRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddFacebookChannel(
        this IServiceCollection services,
        FacebookOptions options)
    {
        new FacebookRegistrar().Register(services,
            new NotifyOptions { Facebook = options });
        return services;
    }
}
