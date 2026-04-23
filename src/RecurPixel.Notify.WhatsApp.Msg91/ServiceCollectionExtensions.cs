using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the MSG91 WhatsApp channel.
/// </summary>
public static class Msg91WhatsAppServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.Msg91WhatsAppChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="Msg91WhatsAppRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddMsg91WhatsAppChannel(
        this IServiceCollection services,
        Msg91WhatsAppOptions options)
    {
        new Msg91WhatsAppRegistrar().Register(services,
            new NotifyOptions { WhatsApp = new WhatsAppOptions { Msg91 = options } });
        return services;
    }
}
