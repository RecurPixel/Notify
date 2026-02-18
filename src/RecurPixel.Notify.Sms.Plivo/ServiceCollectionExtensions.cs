using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Sms.Plivo;

/// <summary>
/// DI registration extensions for the Plivo SMS channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="PlivoChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The Plivo options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddPlivoChannel(
        this IServiceCollection services,
        PlivoOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<PlivoChannel>();

        services.AddKeyedSingleton<INotificationChannel, PlivoChannel>("sms:plivo");

        return services;
    }
}
