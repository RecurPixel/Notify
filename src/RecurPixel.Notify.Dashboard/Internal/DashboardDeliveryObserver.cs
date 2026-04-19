using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RecurPixel.Notify.Dashboard;

/// <summary>
/// Implements <see cref="INotifyDeliveryObserver"/> to write every send attempt
/// to the registered <see cref="INotificationLogStore"/>.
/// Registered as a singleton; creates a DI scope per invocation so the store
/// may be scoped (e.g. wrapping a DbContext).
/// </summary>
internal sealed class DashboardDeliveryObserver : INotifyDeliveryObserver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DashboardDeliveryObserver> _logger;

    public DashboardDeliveryObserver(
        IServiceProvider serviceProvider,
        ILogger<DashboardDeliveryObserver> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task OnDeliveryAsync(NotifyResult result, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetService<INotificationLogStore>();

        if (store is null)
        {
            _logger.LogDebug(
                "Dashboard: INotificationLogStore is not registered — " +
                "call AddNotifyDashboardEfCore() or register your own INotificationLogStore.");
            return;
        }

        var log = NotificationLog.FromResult(result);
        await store.AddAsync(log, ct);
    }
}
