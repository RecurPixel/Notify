using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Resend email channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.ResendChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection. Delegates to <see cref="ResendRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddResendChannel(
        this IServiceCollection services,
        ResendOptions options)
    {
        new ResendRegistrar().Register(services,
            new NotifyOptions { Email = new EmailOptions { Resend = options } });
        return services;
    }
}
