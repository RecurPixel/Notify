using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Sms.Twilio;

/// <summary>
/// DI registration extensions for the Twilio SMS adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Twilio SMS channel adapter keyed as "sms:twilio".
    /// Called internally by AddRecurPixelNotify() — do not call directly.
    /// </summary>
    public static IServiceCollection AddTwilioSmsChannel(
        this IServiceCollection services,
        TwilioOptions options)
    {
        services.Configure<TwilioOptions>(o =>
        {
            o.AccountSid = options.AccountSid;
            o.AuthToken = options.AuthToken;
            o.FromNumber = options.FromNumber;
        });

        services.TryAddKeyedSingleton<INotificationChannel, TwilioSmsChannel>("sms:twilio");

        return services;
    }
}