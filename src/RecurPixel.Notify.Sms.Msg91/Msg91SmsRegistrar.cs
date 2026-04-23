using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("sms", "msg91")]
internal sealed class Msg91SmsRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Sms?.Msg91?.AuthKey);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Sms!.Msg91!;
        services.Configure<Msg91SmsOptions>(o =>
        {
            o.AuthKey  = opts.AuthKey;
            o.SenderId = opts.SenderId;
            o.Route    = opts.Route;
        });
        services.AddHttpClient("sms:msg91", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, Msg91SmsChannel>("sms:msg91");
    }
}
