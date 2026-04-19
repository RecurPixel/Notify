using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Microsoft Teams notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="TeamsChannel"/> in the DI container keyed as <c>teams:default</c>.
    /// For direct-injection usage without <c>AddRecurPixelNotify()</c>.
    /// When using <c>AddRecurPixelNotify()</c>, this channel is registered automatically via assembly scanning.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The resolved Teams options.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddTeamsChannel(
        this IServiceCollection services,
        TeamsOptions options)
    {
        services.Configure<TeamsOptions>(o =>
        {
            o.WebhookUrl = options.WebhookUrl;
        });

        services.AddHttpClient("teams:default", http =>
        {
            http.Timeout = TimeSpan.FromSeconds(30);
        });

        services.TryAddKeyedSingleton<INotificationChannel, TeamsChannel>("teams:default");

        return services;
    }
}
