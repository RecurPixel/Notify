using System;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options.Providers;
using Microsoft.Extensions.Options;

namespace RecurPixel.Notify.Push.Fcm;

/// <summary>
/// Registers the FCM push notification channel adapter with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the FCM channel adapter. Registers the adapter under the keyed service key
    /// <c>"push:fcm"</c> so the Orchestrator can resolve it by provider name.
    /// Initialises the Firebase app from <see cref="FcmOptions.ServiceAccountJson"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown at registration time if <see cref="FcmOptions.ServiceAccountJson"/> is empty.
    /// </exception>
    public static IServiceCollection AddRecurPixelFcm(
        this IServiceCollection services,
        FcmOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ServiceAccountJson))
            throw new InvalidOperationException(
                "FcmOptions.ServiceAccountJson is required. " +
                "Provide the full JSON string or an absolute file path.");

        // Initialise Firebase app once at registration time — not at first send
        FirebaseMessagingClient.EnsureInitialized(options.ServiceAccountJson);

        services.Configure<FcmOptions>(o =>
        {
            o.ProjectId          = options.ProjectId;
            o.ServiceAccountJson = options.ServiceAccountJson;
        });

        services.AddSingleton<IFcmMessagingClient, FirebaseMessagingClient>();
        services.AddKeyedSingleton<INotificationChannel, FcmChannel>("push:fcm");

        return services;
    }
}
