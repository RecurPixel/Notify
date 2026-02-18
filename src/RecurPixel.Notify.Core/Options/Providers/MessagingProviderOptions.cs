namespace RecurPixel.Notify.Core.Options.Providers;

/// <summary>Meta Cloud API credentials for WhatsApp.</summary>
public class MetaCloudOptions
{
    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
}

/// <summary>Slack credentials. WebhookUrl for simple posting, BotToken for Bot API.</summary>
public class SlackOptions
{
    public string? WebhookUrl { get; set; }
    public string? BotToken { get; set; }
}

/// <summary>Discord webhook credentials.</summary>
public class DiscordOptions
{
    public string WebhookUrl { get; set; } = string.Empty;
}

/// <summary>Microsoft Teams webhook credentials.</summary>
public class TeamsOptions
{
    public string WebhookUrl { get; set; } = string.Empty;
}

/// <summary>Telegram Bot API credentials.</summary>
public class TelegramOptions
{
    public string BotToken { get; set; } = string.Empty;
    /// <summary>Default chat ID. Can be overridden per send via NotificationPayload.To.</summary>
    public string? ChatId { get; set; }
    /// <summary>Optional: "HTML" or "MarkdownV2". Null = plain text.</summary>
    public string? ParseMode { get; set; }
}

/// <summary>Meta Messenger API credentials.</summary>
public class FacebookOptions
{
    public string PageAccessToken { get; set; } = string.Empty;
}