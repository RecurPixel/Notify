using System.Collections.Generic;

namespace RecurPixel.Notify;

/// <summary>
/// A strongly-typed in-app notification passed to the <c>OnDeliver</c> handler.
/// Mapped from <see cref="NotificationPayload"/> by <c>InAppChannel</c> before invoking the handler.
/// </summary>
public sealed class InAppNotification
{
    /// <summary>
    /// The recipient user ID. Mapped from <see cref="NotificationPayload.To"/>.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// The notification title or subject. Mapped from <see cref="NotificationPayload.Subject"/>.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// The notification body. Mapped from <see cref="NotificationPayload.Body"/>.
    /// </summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>
    /// Channel-specific extras. Mapped from <see cref="NotificationPayload.Metadata"/>.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
