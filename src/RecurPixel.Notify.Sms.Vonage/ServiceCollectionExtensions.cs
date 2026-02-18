using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Sms.Vonage;

/// <summary>
/// DI registration extensions for the Vonage SMS channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="VonageSmsChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The Vonage options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddVonageSmsChannel(
        this IServiceCollection services,
        VonageOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<VonageSmsChannel>();

        services.AddKeyedSingleton<INotificationChannel, VonageSmsChannel>("sms:vonage");

        return services;
    }
}
