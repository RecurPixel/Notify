using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("email", "mailgun")]
internal sealed class MailgunRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Email?.Mailgun?.ApiKey);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Email!.Mailgun!;
        services.Configure<MailgunOptions>(o =>
        {
            o.ApiKey    = opts.ApiKey;
            o.Domain    = opts.Domain;
            o.FromEmail = opts.FromEmail;
            o.FromName  = opts.FromName;
        });
        services.AddHttpClient("email:mailgun", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, MailgunChannel>("email:mailgun");
    }
}
