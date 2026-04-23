using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("line", "default")]
internal sealed class LineRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Line?.ChannelAccessToken);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Line!;
        services.Configure<LineOptions>(o =>
        {
            o.ChannelAccessToken = opts.ChannelAccessToken;
        });
        services.AddHttpClient("line:default", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, LineChannel>("line:default");
    }
}
