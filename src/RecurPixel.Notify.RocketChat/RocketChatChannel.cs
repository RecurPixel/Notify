using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;

namespace RecurPixel.Notify.RocketChat;

/// <summary>
/// Notification channel adapter for Rocket.Chat incoming webhooks.
/// Sends messages via POST to the configured Rocket.Chat webhook URL.
/// Bulk is handled by the base class loop — Rocket.Chat webhooks are per-message.
/// </summary>
public sealed class RocketChatChannel : NotificationChannelBase
{
    private readonly RocketChatOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<RocketChatChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "rocketchat";

    /// <summary>
    /// Initialises a new instance of <see cref="RocketChatChannel"/>.
    /// </summary>
    public RocketChatChannel(
        IOptions<RocketChatOptions> options,
        HttpClient http,
        ILogger<RocketChatChannel> logger)
    {
        _options = options.Value;
        _http    = http;
        _logger  = logger;
    }

    /// <inheritdoc />
    public override async Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "RocketChat: sending message to webhook");

        try
        {
            var text = string.IsNullOrWhiteSpace(payload.Subject)
                ? payload.Body ?? string.Empty
                : $"**{payload.Subject}**\n\n{payload.Body}";

            var body = new RocketChatWebhookRequest
            {
                Text     = text,
                Username = _options.Username,
                Channel  = _options.Channel
            };

            var response = await _http.PostAsJsonAsync(
                _options.WebhookUrl, body, JsonOptions, ct);

            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "RocketChat: send failed. Status {Status}. Body {Body}",
                    (int)response.StatusCode, raw);

                return Fail(payload.To, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            _logger.LogDebug("RocketChat: message sent successfully");

            return new NotifyResult
            {
                Success   = true,
                Channel   = ChannelName,
                Provider  = "rocketchat",
                Recipient = payload.To,
                SentAt    = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "RocketChat: exception sending message");
            return Fail(payload.To, ex.Message);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private NotifyResult Fail(string to, string error) => new()
    {
        Success   = false,
        Channel   = ChannelName,
        Provider  = "rocketchat",
        Recipient = to,
        Error     = error,
        SentAt    = DateTime.UtcNow
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };
}

// ── internal request shape ───────────────────────────────────────────────────

internal sealed class RocketChatWebhookRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("channel")]
    public string? Channel { get; set; }
}
