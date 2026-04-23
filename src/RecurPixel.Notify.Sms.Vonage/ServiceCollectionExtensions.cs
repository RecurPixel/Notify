using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Vonage SMS channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.VonageSmsChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="VonageSmsRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddVonageSmsChannel(
        this IServiceCollection services,
        VonageOptions options)
    {
        new VonageSmsRegistrar().Register(services,
            new NotifyOptions { Sms = new SmsOptions { Vonage = options } });
        return services;
    }
}
