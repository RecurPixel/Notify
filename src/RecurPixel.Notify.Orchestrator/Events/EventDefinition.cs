using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;

namespace RecurPixel.Notify.Orchestrator.Events;

/// <summary>
/// Immutable definition of a named notification event.
/// Created via <see cref="EventDefinitionBuilder"/> and stored in <see cref="EventRegistry"/>.
/// </summary>
internal sealed class EventDefinition
{
    /// <summary>Unique name for this event e.g. "order.placed".</summary>
    public string EventName { get; init; } = string.Empty;

    /// <summary>Ordered list of channel names this event dispatches to e.g. ["email", "sms", "push"].</summary>
    public IReadOnlyList<string> Channels { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Per-channel send conditions. If a channel has a condition and it returns false,
    /// that channel is skipped for this dispatch.
    /// </summary>
    public IReadOnlyDictionary<string, Func<NotifyContext, bool>> Conditions { get; init; }
        = new Dictionary<string, Func<NotifyContext, bool>>();

    /// <summary>
    /// Per-event retry overrides. When null, global retry config is used.
    /// </summary>
    public RetryOptions? Retry { get; init; }

    /// <summary>
    /// Cross-channel fallback chain. If a channel fails after retries, the next channel
    /// in this list is tried. Independent of within-channel provider fallback.
    /// </summary>
    public IReadOnlyList<string>? FallbackChain { get; init; }
}
