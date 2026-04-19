using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Dashboard;

namespace RecurPixel.Notify;

/// <summary>
/// Extension methods for registering the RecurPixel.Notify dashboard data layer.
/// </summary>
public static class DashboardServiceCollectionExtensions
{
    /// <summary>
    /// Registers the RecurPixel.Notify dashboard data layer.
    /// <para>
    /// After this call, every send attempt (success or failure) is automatically persisted
    /// via the registered <see cref="INotificationLogStore"/>. Call
    /// <c>AddNotifyDashboardEfCore()</c> from <c>RecurPixel.Notify.Dashboard.EfCore</c>
    /// to register the built-in EF Core store, or register your own
    /// <see cref="INotificationLogStore"/> implementation.
    /// </para>
    /// <para>
    /// This method wires storage through <see cref="INotifyDeliveryObserver"/> — no changes
    /// to existing <c>AddRecurPixelNotify()</c> or <c>AddRecurPixelNotifyOrchestrator()</c>
    /// calls are required.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure <see cref="DashboardOptions"/>.</param>
    public static IServiceCollection AddNotifyDashboard(
        this IServiceCollection services,
        Action<DashboardOptions>? configure = null)
    {
        var options = new DashboardOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);

        // Register the delivery observer as a singleton — the Orchestrator's NotifyService
        // discovers and invokes all INotifyDeliveryObserver registrations after each send.
        services.AddSingleton<INotifyDeliveryObserver, DashboardDeliveryObserver>();

        return services;
    }
}
