using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Discord;

/// <summary>
/// DI registration extensions for the Discord notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="DiscordChannel"/> in the DI container keyed as <c>discord:default</c>.
    /// For direct-injection usage without <c>AddRecurPixelNotify()</c>.
    /// When using <c>AddRecurPixelNotify()</c>, this channel is registered automatically via assembly scanning.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The resolved Discord options.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddDiscordChannel(
        this IServiceCollection services,
        DiscordOptions options)
    {
        services.Configure<DiscordOptions>(o =>
        {
            o.WebhookUrl = options.WebhookUrl;
        });

        services.AddHttpClient();

        services.TryAddKeyedSingleton<INotificationChannel, DiscordChannel>("discord:default");

        return services;
    }
}
