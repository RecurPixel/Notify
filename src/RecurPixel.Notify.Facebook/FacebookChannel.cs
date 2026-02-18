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
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Facebook;

/// <summary>
/// Notification channel adapter for the Meta Messenger Send API.
/// Sends messages to a recipient via their page-scoped user ID (PSID).
/// </summary>
public sealed class FacebookChannel : NotificationChannelBase
{
    private readonly FacebookOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<FacebookChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "facebook";

    /// <summary>
    /// Initialises a new instance of <see cref="FacebookChannel"/>.
    /// </summary>
    public FacebookChannel(
        IOptions<FacebookOptions> options,
        HttpClient http,
        ILogger<FacebookChannel> logger)
    {
        _options = options.Value;
        _http = http;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Facebook: sending message to PSID {Psid}",
            payload.To);

        try
        {
            var url = $"https://graph.facebook.com/v19.0/me/messages?access_token={_options.PageAccessToken}";

            var body = new FacebookSendRequest
            {
                Recipient = new FacebookRecipient { Id = payload.To },
                Message = new FacebookMessage
                {
                    Text = string.IsNullOrWhiteSpace(payload.Subject)
                               ? payload.Body
                               : $"{payload.Subject}\n\n{payload.Body}"
                }
            };

            var response = await _http.PostAsJsonAsync(url, body, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Facebook: send failed for PSID {Psid}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<FacebookSendResponse>(raw, JsonOptions);
            var messageId = result?.MessageId;

            _logger.LogDebug(
                "Facebook: message sent to PSID {Psid}. MessageId {MessageId}",
                payload.To, messageId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "facebook",
                ProviderId = messageId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Facebook: exception sending to PSID {Psid}",
                payload.To);

            return Fail(payload, ex.Message);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private NotifyResult Fail(NotificationPayload payload, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "facebook",
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

internal sealed class FacebookSendRequest
{
    [JsonPropertyName("recipient")]
    public FacebookRecipient Recipient { get; set; } = new();

    [JsonPropertyName("message")]
    public FacebookMessage Message { get; set; } = new();
}

internal sealed class FacebookRecipient
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

internal sealed class FacebookMessage
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

internal sealed class FacebookSendResponse
{
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    [JsonPropertyName("recipient_id")]
    public string? RecipientId { get; set; }
}
