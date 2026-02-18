namespace RecurPixel.Notify.Core.Options;

/// <summary>
/// Configuration for retry behaviour on failed send attempts.
/// Can be set globally on NotifyOptions or overridden per event.
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Maximum number of send attempts including the first try.
    /// Default: 3
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay in milliseconds between retry attempts.
    /// Default: 500
    /// </summary>
    public int DelayMs { get; set; } = 500;

    /// <summary>
    /// If true, delay doubles on each retry attempt (exponential backoff).
    /// If false, delay is fixed at DelayMs for every retry.
    /// Default: true
    /// </summary>
    public bool ExponentialBackoff { get; set; } = true;
}