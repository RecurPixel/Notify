using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("teams", "default")]
internal sealed class TeamsRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Teams?.WebhookUrl);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Teams!;
        services.Configure<TeamsOptions>(o =>
        {
            o.WebhookUrl = opts.WebhookUrl;
        });
        services.AddHttpClient("teams:default", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, TeamsChannel>("teams:default");
    }
}
