using RecurPixel.Notify.Core.Models;

namespace RecurPixel.Notify.Core.Channels;

/// <summary>
/// Base class for all notification channel adapters.
/// Provides a default SendBulkAsync implementation that loops SendAsync with a concurrency cap.
/// Adapters with native batch APIs override SendBulkAsync.
/// Adapters without native batch APIs extend this class and implement only SendAsync.
/// Never implement INotificationChannel directly — always extend this base class.
/// </summary>
public abstract class NotificationChannelBase : INotificationChannel
{
    /// <inheritdoc />
    public abstract string ChannelName { get; }

    /// <inheritdoc />
    public abstract Task<NotifyResult> SendAsync(NotificationPayload payload, CancellationToken ct = default);

    /// <summary>
    /// Default bulk implementation — loops SendAsync with a concurrency cap.
    /// Override this only if the provider has a native batch API.
    /// The concurrency cap is controlled by <see cref="BulkConcurrencyLimit"/>.
    /// </summary>
    public virtual async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads,
        CancellationToken ct = default)
    {
        var semaphore = new SemaphoreSlim(BulkConcurrencyLimit);

        var tasks = payloads.Select(async payload =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var result = await SendAsync(payload, ct);
                result.Recipient = payload.To;
                return result;
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        return new BulkNotifyResult
        {
            Results = results,
            Channel = ChannelName,
            UsedNativeBatch = false
        };
    }

    /// <summary>
    /// Maximum number of concurrent SendAsync calls in the default bulk loop.
    /// Prevents rate limit violations when looping single-send APIs at scale.
    /// Override per adapter if the provider has a lower or higher rate limit.
    /// Default: 10
    /// </summary>
    protected virtual int BulkConcurrencyLimit => 10;
}