using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Orchestrator.Dispatch;
using RecurPixel.Notify.Orchestrator.Options;
using RecurPixel.Notify.Orchestrator.Services;

namespace RecurPixel.Notify.Orchestrator.Extensions;

/// <summary>
/// Extension methods for registering the RecurPixel.Notify Orchestrator with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Orchestrator, event registry, channel dispatcher, and <see cref="INotifyService"/>.
    /// Call this after <c>AddRecurPixelNotify()</c> from <c>RecurPixel.Notify.Core</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to define events and set the delivery hook.</param>
    public static IServiceCollection AddRecurPixelNotifyOrchestrator(
        this IServiceCollection services,
        Action<OrchestratorOptions>? configure = null)
    {
        var options = new OrchestratorOptions();
        configure?.Invoke(options);

        // Register options and registry as singletons — built once at startup
        services.AddSingleton(options);
        services.AddSingleton(options.Registry);

        // ChannelDispatcher is scoped — resolves scoped IServiceProvider correctly
        services.AddScoped<ChannelDispatcher>();

        // INotifyService is the primary user-facing service
        services.AddScoped<INotifyService, NotifyService>();

        return services;
    }
}
