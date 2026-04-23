using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the LINE Messaging notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.LineChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="LineRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddLineChannel(
        this IServiceCollection services,
        LineOptions options)
    {
        new LineRegistrar().Register(services,
            new NotifyOptions { Line = options });
        return services;
    }
}
