using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// Registers the Twilio WhatsApp channel adapter with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Twilio WhatsApp adapter. Delegates to <see cref="TwilioWhatsAppRegistrar"/>.
    /// Uses named options (<c>"whatsapp:twilio"</c>) to isolate credentials from the SMS adapter.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if any required credential is missing.
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

        new TwilioWhatsAppRegistrar().Register(services,
            new NotifyOptions { WhatsApp = new WhatsAppOptions { Twilio = options } });
        return services;
    }
}
