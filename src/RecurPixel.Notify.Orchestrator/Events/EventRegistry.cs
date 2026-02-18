namespace RecurPixel.Notify.Orchestrator.Events;

/// <summary>
/// Stores and retrieves <see cref="EventDefinition"/> instances by name.
/// Populated at startup via <see cref="Options.OrchestratorOptions.DefineEvent"/>.
/// </summary>
internal sealed class EventRegistry
{
    private readonly Dictionary<string, EventDefinition> _events = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers an event definition. Throws if the name is already registered.</summary>
    internal void Register(EventDefinition definition)
    {
        if (_events.ContainsKey(definition.EventName))
            throw new InvalidOperationException(
                $"Event '{definition.EventName}' is already registered. Each event name must be unique.");
        _events[definition.EventName] = definition;
    }

    /// <summary>Returns the event definition for the given name, or null if not found.</summary>
    internal EventDefinition? Get(string eventName)
        => _events.TryGetValue(eventName, out var def) ? def : null;
}
