using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;

namespace RecurPixel.Notify.Orchestrator.Services;

/// <summary>
/// Primary entry point for notification dispatch. Inject this into your application services.
/// </summary>
public interface INotifyService
{
    // ── Orchestrated single send ─────────────────────────────────────────────

    /// <summary>
    /// Triggers a named event for a single user.
    /// Channels are resolved from the event definition, conditions are evaluated,
    /// and active channels are dispatched in parallel.
    /// The <c>OnDelivery</c> hook is called once per channel result.
    /// </summary>
    /// <param name="eventName">Name of the event as registered via DefineEvent.</param>
    /// <param name="context">User and per-channel payload data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Aggregate result — Success is true only if all active channels succeeded.
    /// Use the OnDelivery hook for per-channel detail.
    /// </returns>
    Task<NotifyResult> TriggerAsync(string eventName, NotifyContext context, CancellationToken ct = default);

    // ── Orchestrated bulk send ───────────────────────────────────────────────

    /// <summary>
    /// Triggers a named event for multiple users in one call.
    /// Each context is an independent user with their own payload data.
    /// Internally calls TriggerAsync per context with a concurrency cap.
    /// The <c>OnDelivery</c> hook is called once per individual channel result.
    /// </summary>
    /// <param name="eventName">Name of the event as registered via DefineEvent.</param>
    /// <param name="contexts">One context per recipient user.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<BulkNotifyResult> BulkTriggerAsync(
        string eventName,
        IReadOnlyList<NotifyContext> contexts,
        CancellationToken ct = default);

    // ── Direct channel access (bypass orchestration) ─────────────────────────

    /// <summary>Email channel — sends directly, bypassing the event system.</summary>
    INotificationChannel Email { get; }

    /// <summary>SMS channel — sends directly, bypassing the event system.</summary>
    INotificationChannel Sms { get; }

    /// <summary>Push notification channel — sends directly, bypassing the event system.</summary>
    INotificationChannel Push { get; }

    /// <summary>WhatsApp channel — sends directly, bypassing the event system.</summary>
    INotificationChannel WhatsApp { get; }

    /// <summary>Slack channel — sends directly, bypassing the event system.</summary>
    INotificationChannel Slack { get; }

    /// <summary>Discord channel — sends directly, bypassing the event system.</summary>
    INotificationChannel Discord { get; }

    /// <summary>Microsoft Teams channel — sends directly, bypassing the event system.</summary>
    INotificationChannel Teams { get; }

    /// <summary>Telegram channel — sends directly, bypassing the event system.</summary>
    INotificationChannel Telegram { get; }

    /// <summary>Facebook Messenger channel — sends directly, bypassing the event system.</summary>
    INotificationChannel Facebook { get; }

    /// <summary>In-app inbox channel — sends directly, bypassing the event system.</summary>
    INotificationChannel InApp { get; }
}
