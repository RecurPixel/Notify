using System;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.WhatsApp.Twilio;

/// <summary>
/// Registers the Twilio WhatsApp channel adapter with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Twilio WhatsApp adapter. Registers under the keyed service key
    /// <c>"whatsapp:twilio"</c> so the Orchestrator can resolve it by provider name.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown at registration time if any required credential is missing.
    /// </exception>
    public static IServiceCollection AddRecurPixelWhatsAppTwilio(
        this IServiceCollection services,
        TwilioOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.AccountSid))
            throw new InvalidOperationException("TwilioOptions.AccountSid is required.");

        if (string.IsNullOrWhiteSpace(options.AuthToken))
            throw new InvalidOperationException("TwilioOptions.AuthToken is required.");

        if (string.IsNullOrWhiteSpace(options.FromNumber))
            throw new InvalidOperationException(
                "TwilioOptions.FromNumber is required. " +
                "Use the whatsapp:+1234567890 format or a plain number — the adapter adds the prefix.");

        services.Configure<TwilioOptions>(o =>
        {
            o.AccountSid = options.AccountSid;
            o.AuthToken  = options.AuthToken;
            o.FromNumber = options.FromNumber;
        });

        services.AddKeyedSingleton<INotificationChannel, TwilioWhatsAppChannel>("whatsapp:twilio");

        return services;
    }
}
