using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("viber", "default")]
internal sealed class ViberRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Viber?.BotAuthToken);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Viber!;
        services.Configure<ViberOptions>(o =>
        {
            o.BotAuthToken    = opts.BotAuthToken;
            o.SenderName      = opts.SenderName;
            o.SenderAvatarUrl = opts.SenderAvatarUrl;
        });
        services.AddHttpClient("viber:default", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, ViberChannel>("viber:default");
    }
}
