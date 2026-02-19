using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Email.Postmark;

/// <summary>
/// DI registration extensions for the Postmark email channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="PostmarkChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The Postmark options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddPostmarkChannel(
        this IServiceCollection services,
        PostmarkOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddHttpClient<PostmarkChannel>();

        services.AddKeyedSingleton<INotificationChannel, PostmarkChannel>("email:postmark");

        return services;
    }
}
