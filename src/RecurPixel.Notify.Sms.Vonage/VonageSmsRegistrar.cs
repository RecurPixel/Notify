using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("sms", "vonage")]
internal sealed class VonageSmsRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Sms?.Vonage?.ApiKey);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Sms!.Vonage!;
        services.Configure<VonageOptions>(o =>
        {
            o.ApiKey     = opts.ApiKey;
            o.ApiSecret  = opts.ApiSecret;
            o.FromNumber = opts.FromNumber;
        });
        services.AddHttpClient("sms:vonage", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, VonageSmsChannel>("sms:vonage");
    }
}
