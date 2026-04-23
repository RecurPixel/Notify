using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Telegram notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.TelegramChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="TelegramRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddTelegramChannel(
        this IServiceCollection services,
        TelegramOptions options)
    {
        new TelegramRegistrar().Register(services,
            new NotifyOptions { Telegram = options });
        return services;
    }
}
