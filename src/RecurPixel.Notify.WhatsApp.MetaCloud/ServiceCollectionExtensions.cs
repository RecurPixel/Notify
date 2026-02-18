using System;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.WhatsApp.MetaCloud;

/// <summary>
/// Registers the Meta WhatsApp Cloud channel adapter with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Meta Cloud WhatsApp adapter. Registers under the keyed service key
    /// <c>"whatsapp:metacloud"</c> so the Orchestrator can resolve it by provider name.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown at registration time if any required credential is missing.
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

        services.Configure<MetaCloudOptions>(o =>
        {
            o.AccessToken   = options.AccessToken;
            o.PhoneNumberId = options.PhoneNumberId;
        });

        services.AddHttpClient(nameof(MetaCloudWhatsAppChannel));
        services.AddKeyedSingleton<INotificationChannel, MetaCloudWhatsAppChannel>("whatsapp:metacloud");

        return services;
    }
}
