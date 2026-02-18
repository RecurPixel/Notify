using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Email.Mailgun;

/// <summary>
/// DI registration extensions for the Mailgun email channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="MailgunChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The Mailgun options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddMailgunChannel(
        this IServiceCollection services,
        MailgunOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<MailgunChannel>();

        services.AddKeyedSingleton<INotificationChannel, MailgunChannel>("email:mailgun");

        return services;
    }
}
