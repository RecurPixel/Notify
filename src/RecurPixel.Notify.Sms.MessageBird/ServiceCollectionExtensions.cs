using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the MessageBird SMS channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.MessageBirdChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="MessageBirdRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddMessageBirdChannel(
        this IServiceCollection services,
        MessageBirdOptions options)
    {
        new MessageBirdRegistrar().Register(services,
            new NotifyOptions { Sms = new SmsOptions { MessageBird = options } });
        return services;
    }
}
