using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("whatsapp", "metacloud")]
internal sealed class MetaCloudRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.WhatsApp?.MetaCloud?.AccessToken);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.WhatsApp!.MetaCloud!;
        services.Configure<MetaCloudOptions>(o =>
        {
            o.AccessToken   = opts.AccessToken;
            o.PhoneNumberId = opts.PhoneNumberId;
        });
        services.AddHttpClient("whatsapp:metacloud", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, MetaCloudWhatsAppChannel>("whatsapp:metacloud");
    }
}
