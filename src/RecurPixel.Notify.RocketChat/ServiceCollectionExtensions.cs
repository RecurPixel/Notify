using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Rocket.Chat notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.RocketChatChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="RocketChatRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddRocketChatChannel(
        this IServiceCollection services,
        RocketChatOptions options)
    {
        new RocketChatRegistrar().Register(services,
            new NotifyOptions { RocketChat = options });
        return services;
    }
}
