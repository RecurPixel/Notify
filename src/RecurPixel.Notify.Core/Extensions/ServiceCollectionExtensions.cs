using Microsoft.Extensions.DependencyInjection;

namespace RecurPixel.Notify;

/// <summary>
/// IServiceCollection extension methods for registering RecurPixel.Notify.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="NotifyOptions"/> with a fluent builder.
    /// Use this for Tier 1 (direct adapter injection without the Orchestrator).
    /// When using the full stack with event dispatch, call
    /// <c>AddRecurPixelNotify(configureOptions, configureOrchestrator)</c>
    /// from the <c>RecurPixel.Notify.Orchestrator.Extensions</c> namespace instead.
    /// </summary>
    public static IServiceCollection AddNotifyOptions(
        this IServiceCollection services,
        Action<NotifyOptions> configure)
    {
        var options = new NotifyOptions();
        configure(options);
        return services.AddNotifyOptions(options);
    }

    /// <summary>
    /// Registers a pre-built <see cref="NotifyOptions"/> object.
    /// Use this when options are loaded from a database, Vault, or any custom source.
    /// When using the full stack with event dispatch, call
    /// <c>AddRecurPixelNotify(configureOptions, configureOrchestrator)</c>
    /// from the <c>RecurPixel.Notify.Orchestrator.Extensions</c> namespace instead.
    /// </summary>
    public static IServiceCollection AddNotifyOptions(
        this IServiceCollection services,
        NotifyOptions options)
    {
        NotifyOptionsValidator.Validate(options);
        services.AddSingleton(options);
        return services;
    }
}
