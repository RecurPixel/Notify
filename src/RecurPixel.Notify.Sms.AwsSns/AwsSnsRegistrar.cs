using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("sms", "awssns")]
internal sealed class AwsSnsRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Sms?.AwsSns?.AccessKey);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Sms!.AwsSns!;
        services.AddSingleton(Options.Create(opts));

        services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
        {
            var credentials = new BasicAWSCredentials(opts.AccessKey, opts.SecretKey);
            var region = Amazon.RegionEndpoint.GetBySystemName(opts.Region);
            return new AmazonSimpleNotificationServiceClient(credentials, region);
        });

        services.AddSingleton<AwsSnsChannel>();
        services.TryAddKeyedSingleton<INotificationChannel, AwsSnsChannel>("sms:awssns");
    }
}
