using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("whatsapp", "twilio")]
internal sealed class TwilioWhatsAppRegistrar : IAdapterRegistrar
{
    internal const string OptionsName = "whatsapp:twilio";

    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.WhatsApp?.Twilio?.AccountSid);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.WhatsApp!.Twilio!;
        services.Configure<TwilioOptions>(OptionsName, o =>
        {
            o.AccountSid = opts.AccountSid;
            o.AuthToken  = opts.AuthToken;
            o.FromNumber = opts.FromNumber;
        });
        services.TryAddKeyedSingleton<INotificationChannel, TwilioWhatsAppChannel>("whatsapp:twilio");
    }
}
