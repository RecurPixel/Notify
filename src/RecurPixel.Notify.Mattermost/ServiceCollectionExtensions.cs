using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;

namespace RecurPixel.Notify.Mattermost;

/// <summary>
/// DI registration extensions for the Mattermost notification channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="MattermostChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The Mattermost options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    internal static IServiceCollection AddMattermostChannel(
        this IServiceCollection services,
        MattermostOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<MattermostChannel>();

        services.AddKeyedSingleton<INotificationChannel, MattermostChannel>(
            "mattermost:mattermost");

        return services;
    }
}
