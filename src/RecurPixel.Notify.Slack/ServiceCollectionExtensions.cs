using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Slack notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SlackChannel"/> in the DI container keyed as <c>slack:default</c>.
    /// For direct-injection usage without <c>AddRecurPixelNotify()</c>.
    /// When using <c>AddRecurPixelNotify()</c>, this channel is registered automatically via assembly scanning.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The resolved Slack options.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddSlackChannel(
        this IServiceCollection services,
        SlackOptions options)
    {
        services.Configure<SlackOptions>(o =>
        {
            o.WebhookUrl = options.WebhookUrl;
            o.BotToken = options.BotToken;
        });

        services.AddHttpClient();

        services.TryAddKeyedSingleton<INotificationChannel, SlackChannel>("slack:default");

        return services;
    }
}
