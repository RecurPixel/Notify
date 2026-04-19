namespace RecurPixel.Notify;

/// <summary>
/// Receives a callback after every notification send attempt.
/// Register implementations in DI — the Orchestrator discovers and invokes all of them.
/// Used by <c>RecurPixel.Notify.Dashboard</c> to auto-wire log storage without requiring
/// changes to <see cref="OrchestratorOptions"/>.
/// </summary>
/// <remarks>
/// Implementations are typically registered as singletons.
/// If your implementation holds scoped resources (e.g. a DbContext), create an
/// <see cref="IServiceProvider"/> scope inside <see cref="OnDeliveryAsync"/>.
/// Exceptions thrown by any observer are caught and logged — they will never
/// propagate back to the caller of <c>TriggerAsync</c>.
/// </remarks>
public interface INotifyDeliveryObserver
{
    /// <summary>
    /// Invoked once per <see cref="NotifyResult"/> after every send attempt,
    /// including fallback attempts and failures.
    /// </summary>
    /// <param name="result">The result of the send attempt.</param>
    /// <param name="ct">Cancellation token.</param>
    Task OnDeliveryAsync(NotifyResult result, CancellationToken ct = default);
}
