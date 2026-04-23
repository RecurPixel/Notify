using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("push", "expo")]
internal sealed class ExpoRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => options.Push?.Expo is not null;

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Push!.Expo!;
        services.Configure<ExpoOptions>(o =>
        {
            o.AccessToken = opts.AccessToken;
        });
        services.AddHttpClient("push:expo", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, ExpoChannel>("push:expo");
    }
}
