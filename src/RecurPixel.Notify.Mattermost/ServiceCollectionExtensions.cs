using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Mattermost notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.MattermostChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>.
    /// Delegates to <see cref="MattermostRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddMattermostChannel(
        this IServiceCollection services,
        MattermostOptions options)
    {
        new MattermostRegistrar().Register(services,
            new NotifyOptions { Mattermost = options });
        return services;
    }
}
