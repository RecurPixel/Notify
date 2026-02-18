using System;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Push.Apns;

/// <summary>
/// Registers the APNs push notification channel adapter with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the APNs channel adapter. Registers the adapter under the keyed service key
    /// <c>"push:apns"</c> so the Orchestrator can resolve it by provider name.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown at registration time if any required APNs credential is missing.
    /// </exception>
    public static IServiceCollection AddRecurPixelApns(
        this IServiceCollection services,
        ApnsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.KeyId))
            throw new InvalidOperationException(
                "ApnsOptions.KeyId is required.");

        if (string.IsNullOrWhiteSpace(options.TeamId))
            throw new InvalidOperationException(
                "ApnsOptions.TeamId is required.");

        if (string.IsNullOrWhiteSpace(options.BundleId))
            throw new InvalidOperationException(
                "ApnsOptions.BundleId is required.");

        if (string.IsNullOrWhiteSpace(options.PrivateKey))
            throw new InvalidOperationException(
                "ApnsOptions.PrivateKey is required. Provide the .p8 file content.");

        services.Configure<ApnsOptions>(o =>
        {
            o.KeyId      = options.KeyId;
            o.TeamId     = options.TeamId;
            o.BundleId   = options.BundleId;
            o.PrivateKey = options.PrivateKey;
        });

        services.AddKeyedSingleton<INotificationChannel, ApnsChannel>("push:apns");

        return services;
    }
}
