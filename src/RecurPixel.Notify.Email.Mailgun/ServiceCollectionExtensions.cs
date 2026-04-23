using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Mailgun email channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.MailgunChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection. Delegates to <see cref="MailgunRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddMailgunChannel(
        this IServiceCollection services,
        MailgunOptions options)
    {
        new MailgunRegistrar().Register(services,
            new NotifyOptions { Email = new EmailOptions { Mailgun = options } });
        return services;
    }
}
