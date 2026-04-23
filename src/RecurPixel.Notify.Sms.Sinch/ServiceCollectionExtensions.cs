using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Sinch SMS channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.SinchChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="SinchRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddSinchChannel(
        this IServiceCollection services,
        SinchOptions options)
    {
        new SinchRegistrar().Register(services,
            new NotifyOptions { Sms = new SmsOptions { Sinch = options } });
        return services;
    }
}
