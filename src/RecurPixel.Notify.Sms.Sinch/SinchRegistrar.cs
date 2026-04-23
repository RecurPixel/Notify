using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("sms", "sinch")]
internal sealed class SinchRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Sms?.Sinch?.ServicePlanId);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Sms!.Sinch!;
        services.Configure<SinchOptions>(o =>
        {
            o.ServicePlanId = opts.ServicePlanId;
            o.ApiToken      = opts.ApiToken;
            o.FromNumber    = opts.FromNumber;
        });
        services.AddHttpClient("sms:sinch", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, SinchChannel>("sms:sinch");
    }
}
