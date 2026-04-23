using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("sms", "twilio")]
internal sealed class TwilioSmsRegistrar : IAdapterRegistrar
{
    internal const string OptionsName = "sms:twilio";

    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Sms?.Twilio?.AccountSid);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Sms!.Twilio!;
        services.Configure<TwilioOptions>(OptionsName, o =>
        {
            o.AccountSid = opts.AccountSid;
            o.AuthToken  = opts.AuthToken;
            o.FromNumber = opts.FromNumber;
        });
        services.TryAddKeyedSingleton<INotificationChannel, TwilioSmsChannel>("sms:twilio");
    }
}
