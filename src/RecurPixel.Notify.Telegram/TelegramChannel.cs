using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Telegram;

/// <summary>
/// Notification channel adapter for Telegram Bot API.
/// Sends messages via POST https://api.telegram.org/bot{token}/sendMessage.
/// </summary>
[ChannelAdapter("telegram", "default")]
public sealed class TelegramChannel : NotificationChannelBase
{
    private readonly TelegramOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "telegram";

    /// <summary>
    /// Initialises a new instance of <see cref="TelegramChannel"/>.
    /// </summary>
    public TelegramChannel(
        IOptions<TelegramOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<TelegramChannel> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Telegram: sending message to chat_id {ChatId}",
            payload.To);

        try
        {
            var chatId = !string.IsNullOrEmpty(payload.To) ? payload.To : _options.ChatId;
            if (string.IsNullOrEmpty(chatId))
                return Fail(payload, "No ChatId provided. Set payload.To or configure a default ChatId in TelegramOptions.");

            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";

            var body = new TelegramSendMessageRequest
            {
                ChatId = chatId,
                Text = string.IsNullOrWhiteSpace(payload.Subject)
                                ? payload.Body
                                : $"{payload.Subject}\n\n{payload.Body}",
                ParseMode = string.IsNullOrWhiteSpace(_options.ParseMode) ? null : _options.ParseMode
            };

            var http = _httpClientFactory.CreateClient();
            var response = await http.PostAsJsonAsync(url, body, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Telegram: send failed for chat_id {ChatId}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<TelegramApiResponse>(raw, JsonOptions);
            var messageId = result?.Result?.MessageId.ToString();

            _logger.LogDebug(
                "Telegram: message sent to chat_id {ChatId}. MessageId {MessageId}",
                payload.To, messageId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "telegram",
                ProviderId = messageId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Telegram: exception sending to chat_id {ChatId}",
                payload.To);

            return Fail(payload, ex.Message);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private NotifyResult Fail(NotificationPayload payload, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "telegram",
        Recipient = payload.To,
        Error = error,
        SentAt = DateTime.UtcNow
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };
}

// ── internal request / response shapes ──────────────────────────────────────

internal sealed class TelegramSendMessageRequest
{
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>Optional: "HTML" or "MarkdownV2". Null = plain text.</summary>
    [JsonPropertyName("parse_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParseMode { get; set; }
}

internal sealed class TelegramApiResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("result")]
    public TelegramMessageResult? Result { get; set; }
}

internal sealed class TelegramMessageResult
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }
}
