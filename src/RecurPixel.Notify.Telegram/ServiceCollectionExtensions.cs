using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Telegram notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="TelegramChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The Telegram options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddTelegramChannel(
        this IServiceCollection services,
        TelegramOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient("telegram:default", http =>
        {
            http.Timeout = TimeSpan.FromSeconds(30);
        });

        services.TryAddKeyedSingleton<INotificationChannel, TelegramChannel>("telegram:default");

        return services;
    }
}
