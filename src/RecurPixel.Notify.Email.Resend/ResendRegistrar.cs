using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("email", "resend")]
internal sealed class ResendRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Email?.Resend?.ApiKey);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Email!.Resend!;
        services.Configure<ResendOptions>(o =>
        {
            o.ApiKey    = opts.ApiKey;
            o.FromEmail = opts.FromEmail;
            o.FromName  = opts.FromName;
        });
        services.AddHttpClient("email:resend", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, ResendChannel>("email:resend");
    }
}
