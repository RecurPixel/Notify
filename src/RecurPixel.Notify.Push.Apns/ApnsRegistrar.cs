using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("push", "apns")]
internal sealed class ApnsRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Push?.Apns?.KeyId);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Push!.Apns!;
        services.Configure<ApnsOptions>(o =>
        {
            o.KeyId      = opts.KeyId;
            o.TeamId     = opts.TeamId;
            o.BundleId   = opts.BundleId;
            o.PrivateKey = opts.PrivateKey;
        });
        services.AddHttpClient("push:apns", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, ApnsChannel>("push:apns");
    }
}
