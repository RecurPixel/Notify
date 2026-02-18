using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Orchestrator.Dispatch;

namespace RecurPixel.Notify.Orchestrator.Services;

/// <summary>
/// An <see cref="INotificationChannel"/> that delegates to <see cref="ChannelDispatcher"/>.
/// Exposed via <see cref="INotifyService"/> direct-channel properties (Email, Sms, Push, etc.).
/// Supports named-provider routing via Metadata["provider"] on the payload.
/// Bulk is handled by the base-class loop — each payload is dispatched independently,
/// which means each can target a different named provider.
/// </summary>
internal sealed class RoutingChannel : NotificationChannelBase
{
    private readonly string _channelName;
    private readonly ChannelDispatcher _dispatcher;

    internal RoutingChannel(string channelName, ChannelDispatcher dispatcher)
    {
        _channelName = channelName;
        _dispatcher = dispatcher;
    }

    /// <inheritdoc/>
    public override string ChannelName => _channelName;

    /// <inheritdoc/>
    public override Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
        => _dispatcher.DispatchAsync(_channelName, payload, retryOptions: null, ct);
}
