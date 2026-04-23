using Azure.Communication.Sms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;
using RecurPixel.Notify.Sms.AzureCommSms;

namespace RecurPixel.Notify;

[ChannelAdapter("sms", "azurecommsms")]
internal sealed class AzureCommSmsRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Sms?.AzureCommSms?.ConnectionString);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Sms!.AzureCommSms!;
        services.AddSingleton(Options.Create(opts));

        services.AddSingleton<IAzureCommSmsClient>(_ =>
            new AzureCommSmsClientWrapper(new SmsClient(opts.ConnectionString)));

        services.AddSingleton<AzureCommSmsChannel>();
        services.TryAddKeyedSingleton<INotificationChannel, AzureCommSmsChannel>("sms:azurecommsms");
    }
}
