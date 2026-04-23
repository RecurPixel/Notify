using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the AWS SES v2 email channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.AwsSesChannel"/> and the <see cref="Amazon.SimpleEmailV2.IAmazonSimpleEmailServiceV2"/>
    /// client. Delegates to <see cref="AwsSesRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddAwsSesChannel(
        this IServiceCollection services,
        AwsSesOptions options)
    {
        new AwsSesRegistrar().Register(services,
            new NotifyOptions { Email = new EmailOptions { AwsSes = options } });
        return services;
    }
}
