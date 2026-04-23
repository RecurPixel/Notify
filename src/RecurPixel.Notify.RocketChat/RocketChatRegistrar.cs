using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("rocketchat", "default")]
internal sealed class RocketChatRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.RocketChat?.WebhookUrl);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.RocketChat!;
        services.Configure<RocketChatOptions>(o =>
        {
            o.WebhookUrl = opts.WebhookUrl;
            o.Username   = opts.Username;
            o.Channel    = opts.Channel;
        });
        services.AddHttpClient("rocketchat:default", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, RocketChatChannel>("rocketchat:default");
    }
}
