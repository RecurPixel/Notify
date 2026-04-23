using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the MSG91 SMS channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.Msg91SmsChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="Msg91SmsRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddMsg91SmsChannel(
        this IServiceCollection services,
        Msg91SmsOptions options)
    {
        new Msg91SmsRegistrar().Register(services,
            new NotifyOptions { Sms = new SmsOptions { Msg91 = options } });
        return services;
    }
}
