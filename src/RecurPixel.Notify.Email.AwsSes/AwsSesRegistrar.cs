using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("email", "awsses")]
internal sealed class AwsSesRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Email?.AwsSes?.AccessKey);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Email!.AwsSes!;
        services.AddSingleton(Options.Create(opts));

        services.AddSingleton<IAmazonSimpleEmailServiceV2>(_ =>
        {
            var credentials = new BasicAWSCredentials(opts.AccessKey, opts.SecretKey);
            var region = Amazon.RegionEndpoint.GetBySystemName(opts.Region);
            return new AmazonSimpleEmailServiceV2Client(credentials, region);
        });

        services.AddSingleton<AwsSesChannel>();
        services.TryAddKeyedSingleton<INotificationChannel, AwsSesChannel>("email:awsses");
    }
}
