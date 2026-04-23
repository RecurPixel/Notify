using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("email", "postmark")]
internal sealed class PostmarkRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Email?.Postmark?.ApiKey);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Email!.Postmark!;
        services.Configure<PostmarkOptions>(o =>
        {
            o.ApiKey    = opts.ApiKey;
            o.FromEmail = opts.FromEmail;
            o.FromName  = opts.FromName;
        });
        services.AddHttpClient("email:postmark", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, PostmarkChannel>("email:postmark");
    }
}
