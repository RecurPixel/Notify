using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;
using RecurPixel.Notify.Push.Fcm;

namespace RecurPixel.Notify;

[ChannelAdapter("push", "fcm")]
internal sealed class FcmRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Push?.Fcm?.ServiceAccountJson);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Push!.Fcm!;

        FirebaseMessagingClient.EnsureInitialized(opts.ServiceAccountJson);

        services.Configure<FcmOptions>(o =>
        {
            o.ProjectId          = opts.ProjectId;
            o.ServiceAccountJson = opts.ServiceAccountJson;
        });

        services.AddSingleton<IFcmMessagingClient, FirebaseMessagingClient>();
        services.TryAddKeyedSingleton<INotificationChannel, FcmChannel>("push:fcm");
    }
}
