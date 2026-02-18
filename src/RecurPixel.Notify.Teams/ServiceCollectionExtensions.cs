using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Teams;

/// <summary>
/// DI registration extensions for the Microsoft Teams notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="TeamsChannel"/> in the DI container keyed as <c>teams:teams</c>.
    /// Called automatically by <c>AddRecurPixelNotify()</c> when Teams options are present.
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

        services.AddHttpClient();

        services.AddKeyedSingleton<INotificationChannel, TeamsChannel>("teams:teams");

        return services;
    }
}
