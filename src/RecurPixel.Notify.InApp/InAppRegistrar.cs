using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("inapp", "default")]
internal sealed class InAppRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => true;

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        services.AddSingleton(Options.Create(new InAppOptions()));
        services.TryAddKeyedSingleton<INotificationChannel, InAppChannel>("inapp:default");
    }
}
