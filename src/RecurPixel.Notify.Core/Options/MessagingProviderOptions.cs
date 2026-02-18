
/// <summary>LINE Messaging API credentials.</summary>
public class LineOptions
{
    /// <summary>Channel Access Token from the LINE Developers console.</summary>
    public string ChannelAccessToken { get; set; } = string.Empty;
}

/// <summary>Viber Business Messages API credentials.</summary>
public class ViberOptions
{
    /// <summary>Bot Authentication Token from the Viber Admin Panel.</summary>
    public string BotAuthToken { get; set; } = string.Empty;

    /// <summary>Display name shown to recipients as the message sender.</summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>Optional avatar URL shown alongside the sender name.</summary>
    public string? SenderAvatarUrl { get; set; }
}

/// <summary>
/// In-app notification options. The user provides a delegate that is called
/// on every send. Storage, SignalR, queuing — all user-owned.
/// </summary>
public class InAppOptions
{
    /// <summary>
    /// Delegate invoked on every in-app send attempt.
    /// Receives the <see cref="RecurPixel.Notify.Core.Models.NotificationPayload"/> and
    /// must return a <see cref="RecurPixel.Notify.Core.Models.NotifyResult"/>.
    /// </summary>
    public Func<RecurPixel.Notify.Core.Models.NotificationPayload, System.Threading.CancellationToken, System.Threading.Tasks.Task<RecurPixel.Notify.Core.Models.NotifyResult>>? Handler { get; set; }
}
