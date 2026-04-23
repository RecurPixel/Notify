using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// Registers the FCM push notification channel adapter with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the FCM channel adapter. Validates <see cref="FcmOptions.ServiceAccountJson"/>,
    /// then delegates to <see cref="FcmRegistrar"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="FcmOptions.ServiceAccountJson"/> is empty.
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

        new FcmRegistrar().Register(services,
            new NotifyOptions { Push = new PushOptions { Fcm = options } });
        return services;
    }
}
