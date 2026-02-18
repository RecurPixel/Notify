using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;

namespace RecurPixel.Notify.Orchestrator.Events;

/// <summary>
/// Fluent builder for <see cref="EventDefinition"/>.
/// Obtain an instance via <see cref="Options.OrchestratorOptions.DefineEvent"/>.
/// </summary>
public sealed class EventDefinitionBuilder
{
    private readonly string _eventName;
    private readonly List<string> _channels = new();
    private readonly Dictionary<string, Func<NotifyContext, bool>> _conditions = new();
    private RetryOptions? _retry;
    private List<string>? _fallbackChain;

    internal EventDefinitionBuilder(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Event name must not be empty.", nameof(eventName));
        _eventName = eventName;
    }

    /// <summary>
    /// Specifies which channels this event dispatches to.
    /// Channels are dispatched in parallel unless a fallback chain is configured.
    /// </summary>
    /// <param name="channels">Lowercase channel names e.g. "email", "sms", "push".</param>
    public EventDefinitionBuilder UseChannels(params string[] channels)
    {
        _channels.AddRange(channels);
        return this;
    }

    /// <summary>
    /// Adds a per-channel send condition evaluated at dispatch time against the <see cref="NotifyContext"/>.
    /// If the condition returns false the channel is skipped — no send, no hook call, no error.
    /// </summary>
    /// <param name="channel">Channel name this condition applies to.</param>
    /// <param name="condition">Predicate receiving the current <see cref="NotifyContext"/>.</param>
    public EventDefinitionBuilder WithCondition(string channel, Func<NotifyContext, bool> condition)
    {
        _conditions[channel] = condition ?? throw new ArgumentNullException(nameof(condition));
        return this;
    }

    /// <summary>
    /// Overrides global retry settings for this event only.
    /// </summary>
    /// <param name="maxAttempts">Maximum delivery attempts including the first.</param>
    /// <param name="delayMs">Base delay between attempts in milliseconds.</param>
    /// <param name="exponentialBackoff">Whether to double the delay on each subsequent attempt.</param>
    public EventDefinitionBuilder WithRetry(int maxAttempts, int delayMs = 500, bool exponentialBackoff = true)
    {
        _retry = new RetryOptions
        {
            MaxAttempts = maxAttempts,
            DelayMs = delayMs,
            ExponentialBackoff = exponentialBackoff
        };
        return this;
    }

    /// <summary>
    /// Defines a cross-channel fallback chain. If the primary channel fails after all retries,
    /// channels in this list are tried in order until one succeeds.
    /// This is cross-channel fallback — for within-channel provider fallback see EmailOptions.Fallback.
    /// </summary>
    /// <param name="chain">Ordered channel names to try on failure e.g. "whatsapp", "sms", "email".</param>
    public EventDefinitionBuilder WithFallback(params string[] chain)
    {
        _fallbackChain = new List<string>(chain);
        return this;
    }

    internal EventDefinition Build() => new()
    {
        EventName = _eventName,
        Channels = _channels.AsReadOnly(),
        Conditions = _conditions,
        Retry = _retry,
        FallbackChain = _fallbackChain?.AsReadOnly()
    };
}
