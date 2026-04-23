using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("sms", "messagebird")]
internal sealed class MessageBirdRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Sms?.MessageBird?.ApiKey);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Sms!.MessageBird!;
        services.Configure<MessageBirdOptions>(o =>
        {
            o.ApiKey      = opts.ApiKey;
            o.Originator  = opts.Originator;
        });
        services.AddHttpClient("sms:messagebird", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, MessageBirdChannel>("sms:messagebird");
    }
}
