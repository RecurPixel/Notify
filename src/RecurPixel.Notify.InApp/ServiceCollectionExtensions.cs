using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;

namespace RecurPixel.Notify.InApp;

/// <summary>
/// DI registration extensions for the in-app notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="InAppChannel"/> into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The InApp options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInAppChannel(
        this IServiceCollection services,
        InAppOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddKeyedSingleton<INotificationChannel, InAppChannel>("inapp:inapp");

        return services;
    }
}
