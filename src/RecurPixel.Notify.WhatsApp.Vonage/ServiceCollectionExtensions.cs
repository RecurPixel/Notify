using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Vonage WhatsApp channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.VonageWhatsAppChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="VonageWhatsAppRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddVonageWhatsAppChannel(
        this IServiceCollection services,
        VonageWhatsAppOptions options)
    {
        new VonageWhatsAppRegistrar().Register(services,
            new NotifyOptions { WhatsApp = new WhatsAppOptions { Vonage = options } });
        return services;
    }
}
