using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("whatsapp", "vonage")]
internal sealed class VonageWhatsAppRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.WhatsApp?.Vonage?.ApiKey);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.WhatsApp!.Vonage!;
        services.Configure<VonageWhatsAppOptions>(o =>
        {
            o.ApiKey     = opts.ApiKey;
            o.ApiSecret  = opts.ApiSecret;
            o.FromNumber = opts.FromNumber;
        });
        services.AddHttpClient("whatsapp:vonage", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, VonageWhatsAppChannel>("whatsapp:vonage");
    }
}
