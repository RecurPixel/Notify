namespace RecurPixel.Notify.Core.Models;

/// <summary>
/// Represents the recipient of a notification.
/// Passed to the orchestrator so conditions can be evaluated per user.
/// The library reads this — it never stores or modifies it.
/// </summary>
public class NotifyUser
{
    /// <summary>
    /// Your application's user identifier. Used for logging and tracing only.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Email address. Required if email channel is used.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Phone number in E.164 format e.g. +14155552671.
    /// Required if SMS or WhatsApp channel is used.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// FCM or APNs device token for push notifications.
    /// Required if push channel is used.
    /// </summary>
    public string? DeviceToken { get; set; }

    /// <summary>
    /// Whether the user's phone number has been verified.
    /// Used by WithCondition() guards on SMS and WhatsApp channels.
    /// </summary>
    public bool PhoneVerified { get; set; }

    /// <summary>
    /// Whether the user has granted push notification permission.
    /// Used by WithCondition() guards on push channel.
    /// </summary>
    public bool PushEnabled { get; set; }

    /// <summary>
    /// Any additional user attributes your application needs to pass through.
    /// Accessible inside WithCondition() lambda expressions.
    /// </summary>
    public Dictionary<string, object> Extra { get; set; } = new();
}