using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the AWS SNS SMS channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Channels.AwsSnsChannel"/> and the <see cref="Amazon.SimpleNotificationService.IAmazonSimpleNotificationService"/>
    /// client. Delegates to <see cref="AwsSnsRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddAwsSnsChannel(
        this IServiceCollection services,
        AwsSnsOptions options)
    {
        new AwsSnsRegistrar().Register(services,
            new NotifyOptions { Sms = new SmsOptions { AwsSns = options } });
        return services;
    }
}
