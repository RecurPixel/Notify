using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Facebook;

/// <summary>
/// DI registration extensions for the Facebook Messenger notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="FacebookChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The Facebook options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddFacebookChannel(
        this IServiceCollection services,
        FacebookOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<FacebookChannel>();

        services.AddKeyedSingleton<INotificationChannel, FacebookChannel>("facebook:facebook");

        return services;
    }
}
