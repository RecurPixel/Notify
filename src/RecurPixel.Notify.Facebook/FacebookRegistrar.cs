using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("facebook", "default")]
internal sealed class FacebookRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Facebook?.PageAccessToken);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Facebook!;
        services.Configure<FacebookOptions>(o =>
        {
            o.PageAccessToken = opts.PageAccessToken;
        });
        services.AddHttpClient("facebook:default", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, FacebookChannel>("facebook:default");
    }
}
