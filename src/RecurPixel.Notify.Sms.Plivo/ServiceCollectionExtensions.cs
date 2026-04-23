using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Plivo SMS channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.PlivoChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="PlivoRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddPlivoChannel(
        this IServiceCollection services,
        PlivoOptions options)
    {
        new PlivoRegistrar().Register(services,
            new NotifyOptions { Sms = new SmsOptions { Plivo = options } });
        return services;
    }
}
