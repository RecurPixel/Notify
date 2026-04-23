using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Slack notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.SlackChannel"/> in the DI container keyed as <c>slack:default</c>.
    /// Delegates to <see cref="SlackRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddSlackChannel(
        this IServiceCollection services,
        SlackOptions options)
    {
        new SlackRegistrar().Register(services,
            new NotifyOptions { Slack = options });
        return services;
    }
}
