namespace RecurPixel.Notify.Core.Options;

/// <summary>
/// Configuration for bulk and batch notification sending behaviour.
/// </summary>
public class BulkOptions
{
    /// <summary>
    /// Maximum number of concurrent SendAsync calls when using the default loop fallback.
    /// Tune this based on your provider's rate limits.
    /// Default: 10
    /// </summary>
    public int ConcurrencyLimit { get; set; } = 10;

    /// <summary>
    /// Maximum number of payloads per batch when using native batch APIs.
    /// Providers enforce their own limits — this is the ceiling we apply before chunking.
    /// Default: 1000
    /// </summary>
    public int MaxBatchSize { get; set; } = 1000;

    /// <summary>
    /// If total payloads exceed MaxBatchSize, automatically split into multiple API calls.
    /// Default: true
    /// </summary>
    public bool AutoChunk { get; set; } = true;
}