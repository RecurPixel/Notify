using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("push", "onesignal")]
internal sealed class OneSignalRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Push?.OneSignal?.AppId);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Push!.OneSignal!;
        services.Configure<OneSignalOptions>(o =>
        {
            o.AppId  = opts.AppId;
            o.ApiKey = opts.ApiKey;
        });
        services.AddHttpClient("push:onesignal", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, OneSignalChannel>("push:onesignal");
    }
}
