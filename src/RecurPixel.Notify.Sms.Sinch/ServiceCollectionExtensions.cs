using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Sinch SMS channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SinchChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The Sinch options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddSinchChannel(
        this IServiceCollection services,
        SinchOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<SinchChannel>();

        services.TryAddKeyedSingleton<INotificationChannel, SinchChannel>("sms:sinch");

        return services;
    }
}
