using System.Collections.Generic;
using System.Linq;

namespace RecurPixel.Notify.Core.Models;

/// <summary>
/// Result of a single <c>INotifyService.TriggerAsync</c> call.
/// Contains one <see cref="NotifyResult"/> per channel that was dispatched.
/// </summary>
public sealed class TriggerResult
{
    /// <summary>The event name that was triggered.</summary>
    public string EventName { get; init; } = string.Empty;

    /// <summary>
    /// The user ID from <see cref="NotifyContext.User"/>.
    /// Populated for correlation when inspecting <see cref="BulkTriggerResult.Results"/>.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>One result per channel dispatched, in dispatch order.</summary>
    public IReadOnlyList<NotifyResult> ChannelResults { get; init; } = [];

    /// <summary><see langword="true"/> when every channel in <see cref="ChannelResults"/> succeeded.</summary>
    public bool AllSucceeded => ChannelResults.All(r => r.Success);

    /// <summary><see langword="true"/> when at least one channel in <see cref="ChannelResults"/> succeeded.</summary>
    public bool AnySucceeded => ChannelResults.Any(r => r.Success);

    /// <summary>Channel results where <see cref="NotifyResult.Success"/> is <see langword="false"/>.</summary>
    public IReadOnlyList<NotifyResult> Failures => ChannelResults.Where(r => !r.Success).ToList();
}
