using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("slack", "default")]
internal sealed class SlackRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Slack?.WebhookUrl)
           || !string.IsNullOrEmpty(options.Slack?.BotToken);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Slack!;
        services.Configure<SlackOptions>(o =>
        {
            o.WebhookUrl = opts.WebhookUrl;
            o.BotToken   = opts.BotToken;
        });
        services.AddHttpClient("slack:default", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, SlackChannel>("slack:default");
    }
}
