using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Discord notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.DiscordChannel"/> in the DI container keyed as <c>discord:default</c>.
    /// Delegates to <see cref="DiscordRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddDiscordChannel(
        this IServiceCollection services,
        DiscordOptions options)
    {
        new DiscordRegistrar().Register(services,
            new NotifyOptions { Discord = options });
        return services;
    }
}
