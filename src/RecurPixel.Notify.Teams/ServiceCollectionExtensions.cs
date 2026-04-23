using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Microsoft Teams notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.TeamsChannel"/> in the DI container keyed as <c>teams:default</c>.
    /// Delegates to <see cref="TeamsRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddTeamsChannel(
        this IServiceCollection services,
        TeamsOptions options)
    {
        new TeamsRegistrar().Register(services,
            new NotifyOptions { Teams = options });
        return services;
    }
}
