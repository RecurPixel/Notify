using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Sms.AwsSns;

/// <summary>
/// DI registration extensions for the AWS SNS SMS channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AwsSnsChannel"/> and the <see cref="IAmazonSimpleNotificationService"/>
    /// client into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The AWS SNS options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddAwsSnsChannel(
        this IServiceCollection services,
        AwsSnsOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
        {
            var credentials = new BasicAWSCredentials(
                options.AccessKey,
                options.SecretKey);

            var region = Amazon.RegionEndpoint.GetBySystemName(options.Region);

            return new AmazonSimpleNotificationServiceClient(credentials, region);
        });

        services.AddSingleton<AwsSnsChannel>();

        services.AddKeyedSingleton<INotificationChannel, AwsSnsChannel>("sms:awssns");

        return services;
    }
}
