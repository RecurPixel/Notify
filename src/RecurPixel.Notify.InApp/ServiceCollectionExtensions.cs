using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;

namespace RecurPixel.Notify.InApp;

/// <summary>
/// DI registration extensions for the in-app notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="InAppChannel"/> and wires the delivery handler.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">
    /// Action to configure <see cref="InAppOptions"/>.
    /// Call <c>OnDeliver</c> inside this action to register the delivery handler.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInAppChannel(
        this IServiceCollection services,
        Action<InAppOptions> configure)
    {
        var options = new InAppOptions();
        configure(options);

        services.AddSingleton(Options.Create(options));
        services.TryAddKeyedSingleton<INotificationChannel, InAppChannel>("inapp:default");

        return services;
    }
}
