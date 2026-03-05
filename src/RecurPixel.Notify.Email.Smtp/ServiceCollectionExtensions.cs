using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the SMTP email adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SMTP email channel adapter keyed as "email:smtp".
    /// Called internally by AddRecurPixelNotify() — do not call directly.
    /// </summary>
    public static IServiceCollection AddSmtpChannel(
        this IServiceCollection services,
        SmtpOptions options)
    {
        services.Configure<SmtpOptions>(o =>
        {
            o.Host = options.Host;
            o.Port = options.Port;
            o.Username = options.Username;
            o.Password = options.Password;
            o.UseSsl = options.UseSsl;
            o.FromEmail = options.FromEmail;
            o.FromName = options.FromName;
        });

        services.TryAddKeyedSingleton<INotificationChannel, SmtpChannel>("email:smtp");

        return services;
    }
}