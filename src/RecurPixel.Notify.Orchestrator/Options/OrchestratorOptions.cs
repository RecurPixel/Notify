using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace RecurPixel.Notify;

/// <summary>
/// Configuration for the RecurPixel.Notify Orchestrator.
/// Pass an <see cref="Action{OrchestratorOptions}"/> to <c>AddRecurPixelNotifyOrchestrator()</c>.
/// </summary>
public sealed class OrchestratorOptions
{
    private readonly EventRegistry _registry = new();
    private readonly List<Func<NotifyResult, IServiceProvider, Task>> _deliveryHandlers = new();

    /// <summary>
    /// Defines a named notification event with its channels, conditions, retry, and fallback config.
    /// </summary>
    /// <param name="eventName">Unique event name e.g. "order.placed".</param>
    /// <param name="configure">Builder action to configure the event.</param>
    public OrchestratorOptions DefineEvent(string eventName, Action<EventDefinitionBuilder> configure)
    {
        var builder = new EventDefinitionBuilder(eventName);
        configure(builder);
        _registry.Register(builder.Build());
        return this;
    }

    /// <summary>
    /// Registers a delivery callback invoked after every send attempt (both success and failure).
    /// Multiple calls are additive — all registered handlers fire in registration order.
    /// Use this for logging, metrics, or audit work that does not require a scoped service.
    /// Called once per <see cref="NotifyResult"/>, never once per bulk batch.
    /// </summary>
    /// <param name="handler">Async callback receiving the <see cref="NotifyResult"/>.</param>
    public OrchestratorOptions OnDelivery(Func<NotifyResult, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _deliveryHandlers.Add((result, _) => handler(result));
        return this;
    }

    /// <summary>
    /// Registers a delivery callback that receives a scoped <typeparamref name="TService"/> resolved
    /// from a new DI scope for each invocation.
    /// Multiple calls are additive — all registered handlers fire in registration order.
    /// Use this to inject a scoped <c>DbContext</c> or other scoped service for audit logging.
    /// </summary>
    /// <typeparam name="TService">A scoped service registered in the DI container.</typeparam>
    /// <param name="handler">Async callback receiving the result and the resolved service.</param>
    public OrchestratorOptions OnDelivery<TService>(Func<NotifyResult, TService, Task> handler)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        _deliveryHandlers.Add(async (result, sp) =>
        {
            using var scope = sp.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<TService>();
            await handler(result, service);
        });
        return this;
    }

    /// <summary>Invokes all registered delivery handlers in registration order.</summary>
    internal async Task InvokeDeliveryHandlers(NotifyResult result, IServiceProvider serviceProvider)
    {
        foreach (var handler in _deliveryHandlers)
            await handler(result, serviceProvider);
    }

    internal bool HasDeliveryHandlers => _deliveryHandlers.Count > 0;

    internal EventRegistry Registry => _registry;
}
