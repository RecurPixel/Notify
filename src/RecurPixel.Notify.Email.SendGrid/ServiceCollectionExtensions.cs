using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Email.SendGrid;

/// <summary>
/// DI registration extensions for the SendGrid email adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SendGrid email channel adapter keyed as "email:sendgrid".
    /// Called internally by AddRecurPixelNotify() — do not call directly.
    /// </summary>
    internal static IServiceCollection AddSendGridChannel(
        this IServiceCollection services,
        SendGridOptions options)
    {
        services.Configure<SendGridOptions>(o =>
        {
            o.ApiKey = options.ApiKey;
            o.FromEmail = options.FromEmail;
            o.FromName = options.FromName;
        });

        services.AddKeyedSingleton<INotificationChannel, SendGridChannel>("email:sendgrid");

        return services;
    }
}