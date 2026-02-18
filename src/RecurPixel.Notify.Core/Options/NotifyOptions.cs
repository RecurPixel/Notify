using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Channels;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Core.Options;

/// <summary>
/// Root configuration object for RecurPixel.Notify.
/// Every property is nullable — only configure the channels you use.
/// Passed to AddRecurPixelNotify() via IConfiguration, fluent builder, or raw POCO.
/// </summary>
public class NotifyOptions
{
    // ── Channels ──────────────────────────────────────────────────────────────
    public EmailOptions? Email { get; set; }
    public SmsOptions? Sms { get; set; }
    public PushOptions? Push { get; set; }
    public WhatsAppOptions? WhatsApp { get; set; }
    public SlackOptions? Slack { get; set; }
    public DiscordOptions? Discord { get; set; }
    public TeamsOptions? Teams { get; set; }
    public TelegramOptions? Telegram { get; set; }
    public FacebookOptions? Facebook { get; set; }
    public LineOptions? Line { get; set; }
    public ViberOptions? Viber { get; set; }
    public InAppOptions? InApp { get; set; }
    public MattermostOptions? Mattermost { get; set; }
    public RocketChatOptions? RocketChat { get; set; }

    // ── Global behaviour ──────────────────────────────────────────────────────
    /// <summary>Global retry settings. Can be overridden per event in the orchestrator.</summary>
    public RetryOptions? Retry { get; set; }

    /// <summary>Global cross-channel fallback chain. Can be overridden per event.</summary>
    public FallbackOptions? Fallback { get; set; }

    /// <summary>Bulk and batch send behaviour settings.</summary>
    public BulkOptions? Bulk { get; set; }

    // ── Delivery hook ─────────────────────────────────────────────────────────
    /// <summary>
    /// Called after every send attempt — single and bulk.
    /// Use this to write to your notification log table.
    /// The library never stores results — that is entirely your responsibility.
    /// </summary>
    public Func<NotifyResult, Task>? OnDelivery { get; set; }
}