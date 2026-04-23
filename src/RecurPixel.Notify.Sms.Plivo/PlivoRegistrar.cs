using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("sms", "plivo")]
internal sealed class PlivoRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Sms?.Plivo?.AuthId);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Sms!.Plivo!;
        services.Configure<PlivoOptions>(o =>
        {
            o.AuthId     = opts.AuthId;
            o.AuthToken  = opts.AuthToken;
            o.FromNumber = opts.FromNumber;
        });
        services.AddHttpClient("sms:plivo", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, PlivoChannel>("sms:plivo");
    }
}
