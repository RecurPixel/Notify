using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Options;

namespace RecurPixel.Notify.Core.Extensions;

/// <summary>
/// IServiceCollection extension methods for registering RecurPixel.Notify.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers RecurPixel.Notify with a fluent options builder.
    /// Use this when you want to configure options programmatically or mix config sources.
    /// </summary>
    public static IServiceCollection AddRecurPixelNotify(
        this IServiceCollection services,
        Action<NotifyOptions> configure)
    {
        var options = new NotifyOptions();
        configure(options);
        return services.AddRecurPixelNotify(options);
    }

    /// <summary>
    /// Registers RecurPixel.Notify with a pre-built options object.
    /// Use this when options are loaded from a database, Vault, or any custom source.
    /// </summary>
    public static IServiceCollection AddRecurPixelNotify(
        this IServiceCollection services,
        NotifyOptions options)
    {
        NotifyOptionsValidator.Validate(options);
        services.AddSingleton(options);
        return services;
    }
}