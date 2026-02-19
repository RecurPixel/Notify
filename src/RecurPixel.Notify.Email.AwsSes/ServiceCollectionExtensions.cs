using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Email.AwsSes;

/// <summary>
/// DI registration extensions for the AWS SES v2 email channel.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AwsSesChannel"/> and the <see cref="IAmazonSimpleEmailServiceV2"/>
    /// client into the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The AWS SES options resolved from <see cref="NotifyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddAwsSesChannel(
        this IServiceCollection services,
        AwsSesOptions options)
    {
        services.AddSingleton(Options.Create(options));

        services.AddSingleton<IAmazonSimpleEmailServiceV2>(_ =>
        {
            var credentials = new BasicAWSCredentials(
                options.AccessKey,
                options.SecretKey);

            var region = Amazon.RegionEndpoint.GetBySystemName(options.Region);

            return new AmazonSimpleEmailServiceV2Client(credentials, region);
        });

        services.AddSingleton<AwsSesChannel>();

        services.AddKeyedSingleton<INotificationChannel, AwsSesChannel>("email:awsses");

        return services;
    }
}
