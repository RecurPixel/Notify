using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// Registers the Meta WhatsApp Cloud channel adapter with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Meta Cloud WhatsApp adapter. Delegates to <see cref="MetaCloudRegistrar"/>.
    /// Registers under the keyed service key <c>"whatsapp:metacloud"</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if any required credential is missing.
    /// </exception>
    public static IServiceCollection AddRecurPixelWhatsAppMetaCloud(
        this IServiceCollection services,
        MetaCloudOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.AccessToken))
            throw new InvalidOperationException("MetaCloudOptions.AccessToken is required.");

        if (string.IsNullOrWhiteSpace(options.PhoneNumberId))
            throw new InvalidOperationException("MetaCloudOptions.PhoneNumberId is required.");

        new MetaCloudRegistrar().Register(services,
            new NotifyOptions { WhatsApp = new WhatsAppOptions { MetaCloud = options } });
        return services;
    }
}
