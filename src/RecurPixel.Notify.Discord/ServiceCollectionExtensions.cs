using Microsoft.Extensions.DependencyInjection;
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
    /// Registers <see cref="DiscordChannel"/> in the DI container keyed as <c>discord:discord</c>.
    /// Called automatically by <c>AddRecurPixelNotify()</c> when Discord options are present.
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

        services.AddKeyedSingleton<INotificationChannel, DiscordChannel>("discord:discord");

        return services;
    }
}
