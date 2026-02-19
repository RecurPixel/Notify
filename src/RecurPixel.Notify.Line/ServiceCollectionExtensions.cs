using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;

namespace RecurPixel.Notify.Line;

/// <summary>
/// DI registration extensions for the LINE Messaging notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="LineChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The LINE options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddLineChannel(
        this IServiceCollection services,
        LineOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<LineChannel>();

        services.AddKeyedSingleton<INotificationChannel, LineChannel>("line:line");

        return services;
    }
}
