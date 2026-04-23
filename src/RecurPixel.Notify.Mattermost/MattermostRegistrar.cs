using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("mattermost", "default")]
internal sealed class MattermostRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Mattermost?.WebhookUrl);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Mattermost!;
        services.Configure<MattermostOptions>(o =>
        {
            o.WebhookUrl = opts.WebhookUrl;
            o.Username   = opts.Username;
            o.Channel    = opts.Channel;
        });
        services.AddHttpClient("mattermost:default", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, MattermostChannel>("mattermost:default");
    }
}
