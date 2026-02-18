using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Slack;

/// <summary>
/// DI registration extensions for the Slack notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SlackChannel"/> in the DI container keyed as <c>slack:slack</c>.
    /// Called automatically by <c>AddRecurPixelNotify()</c> when Slack options are present.
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

        services.AddKeyedSingleton<INotificationChannel, SlackChannel>("slack:slack");

        return services;
    }
}
