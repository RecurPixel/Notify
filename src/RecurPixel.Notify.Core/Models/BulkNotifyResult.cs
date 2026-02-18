namespace RecurPixel.Notify.Core.Models;

/// <summary>
/// Represents the outcome of a bulk notification send operation.
/// Contains one NotifyResult per input payload, in the same order as the inputs.
/// </summary>
public class BulkNotifyResult
{
    /// <summary>
    /// All individual send results, in the same order as the input payloads.
    /// </summary>
    public IReadOnlyList<NotifyResult> Results { get; init; } = Array.Empty<NotifyResult>();

    /// <summary>
    /// True only if every individual send succeeded.
    /// </summary>
    public bool AllSucceeded => Results.All(r => r.Success);

    /// <summary>
    /// True if at least one individual send succeeded.
    /// </summary>
    public bool AnySucceeded => Results.Any(r => r.Success);

    /// <summary>
    /// All failed results. Empty list if everything succeeded.
    /// </summary>
    public IReadOnlyList<NotifyResult> Failures => Results.Where(r => !r.Success).ToList();

    /// <summary>
    /// Total number of send attempts in this batch.
    /// </summary>
    public int Total => Results.Count;

    /// <summary>
    /// Number of successful sends.
    /// </summary>
    public int SuccessCount => Results.Count(r => r.Success);

    /// <summary>
    /// Number of failed sends.
    /// </summary>
    public int FailureCount => Results.Count(r => !r.Success);

    /// <summary>
    /// The channel name for this batch. Same for all results.
    /// </summary>
    public string Channel { get; init; } = string.Empty;

    /// <summary>
    /// True if the adapter used a native batch API for this operation.
    /// False if the base class default loop was used.
    /// </summary>
    public bool UsedNativeBatch { get; init; }
}