using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("discord", "default")]
internal sealed class DiscordRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Discord?.WebhookUrl);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Discord!;
        services.Configure<DiscordOptions>(o =>
        {
            o.WebhookUrl = opts.WebhookUrl;
        });
        services.AddHttpClient("discord:default", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, DiscordChannel>("discord:default");
    }
}
