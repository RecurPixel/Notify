using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// Registers the APNs push notification channel adapter with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the APNs channel adapter. Delegates to <see cref="ApnsRegistrar"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if any required APNs credential is missing.
    /// </exception>
    public static IServiceCollection AddRecurPixelApns(
        this IServiceCollection services,
        ApnsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.KeyId))
            throw new InvalidOperationException("ApnsOptions.KeyId is required.");

        if (string.IsNullOrWhiteSpace(options.TeamId))
            throw new InvalidOperationException("ApnsOptions.TeamId is required.");

        if (string.IsNullOrWhiteSpace(options.BundleId))
            throw new InvalidOperationException("ApnsOptions.BundleId is required.");

        if (string.IsNullOrWhiteSpace(options.PrivateKey))
            throw new InvalidOperationException("ApnsOptions.PrivateKey is required. Provide the .p8 file content.");

        new ApnsRegistrar().Register(services,
            new NotifyOptions { Push = new PushOptions { Apns = options } });
        return services;
    }
}
