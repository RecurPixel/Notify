namespace RecurPixel.Notify;

/// <summary>
/// Aggregated statistics for the dashboard summary row.
/// Returned by <see cref="INotificationLogStore.GetTodayStatsAsync"/>.
/// </summary>
public class NotificationLogStats
{
    /// <summary>Total send attempts in the reporting window.</summary>
    public int TotalSent { get; set; }

    /// <summary>Number of successful send attempts.</summary>
    public int SuccessCount { get; set; }

    /// <summary>Number of failed send attempts.</summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Success rate as a percentage (0–100).
    /// Zero when <see cref="TotalSent"/> is zero.
    /// </summary>
    public double SuccessRate => TotalSent == 0 ? 0 : Math.Round(SuccessCount * 100.0 / TotalSent, 1);

    /// <summary>Number of distinct channels that had at least one send attempt in the window.</summary>
    public int ActiveChannelCount { get; set; }
}
