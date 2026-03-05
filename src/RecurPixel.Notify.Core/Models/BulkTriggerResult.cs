using System.Collections.Generic;
using System.Linq;

namespace RecurPixel.Notify.Core.Models;

/// <summary>
/// Result of a <c>INotifyService.BulkTriggerAsync</c> call.
/// Contains one <see cref="TriggerResult"/> per <see cref="NotifyContext"/>
/// input, preserving input order.
/// </summary>
public sealed class BulkTriggerResult
{
    /// <summary>One <see cref="TriggerResult"/> per input context, in input order.</summary>
    public IReadOnlyList<TriggerResult> Results { get; init; } = [];

    /// <summary><see langword="true"/> when every result in <see cref="Results"/> has <see cref="TriggerResult.AllSucceeded"/>.</summary>
    public bool AllSucceeded => Results.All(r => r.AllSucceeded);

    /// <summary><see langword="true"/> when at least one result in <see cref="Results"/> has <see cref="TriggerResult.AnySucceeded"/>.</summary>
    public bool AnySucceeded => Results.Any(r => r.AnySucceeded);

    /// <summary>Total number of contexts processed.</summary>
    public int Total => Results.Count;

    /// <summary>Number of contexts where all channels succeeded.</summary>
    public int SuccessCount => Results.Count(r => r.AllSucceeded);

    /// <summary>Number of contexts where at least one channel failed.</summary>
    public int FailureCount => Results.Count(r => !r.AllSucceeded);

    /// <summary>Contexts where at least one channel failed.</summary>
    public IReadOnlyList<TriggerResult> Failures => Results.Where(r => !r.AllSucceeded).ToList();
}
