using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the Twilio SMS adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Twilio SMS channel adapter. Delegates to <see cref="TwilioSmsRegistrar"/>.
    /// Uses named options (<c>"sms:twilio"</c>) to isolate credentials from the WhatsApp adapter.
    /// </summary>
    public static IServiceCollection AddTwilioSmsChannel(
        this IServiceCollection services,
        TwilioOptions options)
    {
        new TwilioSmsRegistrar().Register(services,
            new NotifyOptions { Sms = new SmsOptions { Twilio = options } });
        return services;
    }
}
