using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Postmark email channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.PostmarkChannel"/> and its typed <see cref="System.Net.Http.HttpClient"/>
    /// into the service collection. Delegates to <see cref="PostmarkRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddPostmarkChannel(
        this IServiceCollection services,
        PostmarkOptions options)
    {
        new PostmarkRegistrar().Register(services,
            new NotifyOptions { Email = new EmailOptions { Postmark = options } });
        return services;
    }
}
