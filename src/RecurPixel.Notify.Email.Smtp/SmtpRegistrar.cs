using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("email", "smtp")]
internal sealed class SmtpRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Email?.Smtp?.Host);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Email!.Smtp!;
        services.Configure<SmtpOptions>(o =>
        {
            o.Host      = opts.Host;
            o.Port      = opts.Port;
            o.Username  = opts.Username;
            o.Password  = opts.Password;
            o.UseSsl    = opts.UseSsl;
            o.FromEmail = opts.FromEmail;
            o.FromName  = opts.FromName;
        });
        services.TryAddKeyedSingleton<INotificationChannel, SmtpChannel>("email:smtp");
    }
}
