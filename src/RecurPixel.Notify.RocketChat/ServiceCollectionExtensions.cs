using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;

namespace RecurPixel.Notify.RocketChat;

/// <summary>
/// DI registration extensions for the Rocket.Chat notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="RocketChatChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The Rocket.Chat options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddRocketChatChannel(
        this IServiceCollection services,
        RocketChatOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<RocketChatChannel>();

        services.AddKeyedSingleton<INotificationChannel, RocketChatChannel>(
            "rocketchat:rocketchat");

        return services;
    }
}
