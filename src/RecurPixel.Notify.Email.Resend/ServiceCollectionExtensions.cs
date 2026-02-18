using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Email.Resend;

/// <summary>
/// DI registration extensions for the Resend email channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ResendChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The Resend options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddResendChannel(
        this IServiceCollection services,
        ResendOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<ResendChannel>();

        services.AddKeyedSingleton<INotificationChannel, ResendChannel>("email:resend");

        return services;
    }
}
