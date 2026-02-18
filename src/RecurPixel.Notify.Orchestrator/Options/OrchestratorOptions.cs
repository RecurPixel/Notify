using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Orchestrator.Events;

namespace RecurPixel.Notify.Orchestrator.Options;

/// <summary>
/// Configuration for the RecurPixel.Notify Orchestrator.
/// Pass an <see cref="Action{OrchestratorOptions}"/> to <c>AddRecurPixelNotifyOrchestrator()</c>.
/// </summary>
public sealed class OrchestratorOptions
{
    private readonly EventRegistry _registry = new();

    /// <summary>
    /// Optional callback invoked after every individual send attempt — both success and failure.
    /// Use this to write to your notification log table.
    /// Called once per <see cref="NotifyResult"/>, never once per bulk batch.
    /// </summary>
    public Func<NotifyResult, Task>? DeliveryHook { get; private set; }

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
    /// Registers the delivery callback invoked after every send attempt.
    /// </summary>
    /// <param name="hook">Async callback receiving the <see cref="NotifyResult"/>.</param>
    public OrchestratorOptions OnDelivery(Func<NotifyResult, Task> hook)
    {
        DeliveryHook = hook ?? throw new ArgumentNullException(nameof(hook));
        return this;
    }

    internal EventRegistry Registry => _registry;
}
