using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;

namespace RecurPixel.Notify.Viber;

/// <summary>
/// DI registration extensions for the Viber Business Messages notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ViberChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The Viber options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddViberChannel(
        this IServiceCollection services,
        ViberOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<ViberChannel>();

        services.AddKeyedSingleton<INotificationChannel, ViberChannel>("viber:viber");

        return services;
    }
}
