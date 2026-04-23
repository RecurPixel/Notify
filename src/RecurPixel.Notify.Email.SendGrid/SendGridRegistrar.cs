using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("email", "sendgrid")]
internal sealed class SendGridRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Email?.SendGrid?.ApiKey);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Email!.SendGrid!;
        services.Configure<SendGridOptions>(o =>
        {
            o.ApiKey    = opts.ApiKey;
            o.FromEmail = opts.FromEmail;
            o.FromName  = opts.FromName;
        });
        services.TryAddKeyedSingleton<INotificationChannel, SendGridChannel>("email:sendgrid");
    }
}
