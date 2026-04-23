using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("whatsapp", "msg91")]
internal sealed class Msg91WhatsAppRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.WhatsApp?.Msg91?.AuthKey);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.WhatsApp!.Msg91!;
        services.Configure<Msg91WhatsAppOptions>(o =>
        {
            o.AuthKey          = opts.AuthKey;
            o.IntegratedNumber = opts.IntegratedNumber;
            o.Namespace        = opts.Namespace;
        });
        services.AddHttpClient("whatsapp:msg91", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, Msg91WhatsAppChannel>("whatsapp:msg91");
    }
}
