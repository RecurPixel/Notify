using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;

namespace RecurPixel.Notify.Viber;

/// <summary>
/// Notification channel adapter for the Viber Business Messages API.
/// Sends messages via POST https://chatapi.viber.com/pa/send_message.
/// </summary>
public sealed class ViberChannel : NotificationChannelBase
{
    private readonly ViberOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<ViberChannel> _logger;

    private const string SendEndpoint = "https://chatapi.viber.com/pa/send_message";

    /// <inheritdoc />
    public override string ChannelName => "viber";

    /// <summary>
    /// Initialises a new instance of <see cref="ViberChannel"/>.
    /// </summary>
    public ViberChannel(
        IOptions<ViberOptions> options,
        HttpClient http,
        ILogger<ViberChannel> logger)
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
            "Viber: sending message to user {UserId}",
            payload.To);

        try
        {
            var body = new ViberSendRequest
            {
                Receiver = payload.To,
                Sender = new ViberSender
                {
                    Name = _options.SenderName,
                    Avatar = _options.SenderAvatarUrl
                },
                Type = "text",
                Text = string.IsNullOrWhiteSpace(payload.Subject)
                           ? payload.Body
                           : $"{payload.Subject}\n\n{payload.Body}"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint);
            request.Headers.Add("X-Viber-Auth-Token", _options.BotAuthToken);
            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await _http.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Viber: send failed for user {UserId}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<ViberSendResponse>(raw, JsonOptions);

            // Viber status 0 = success. Non-zero = failure even on HTTP 200.
            if (result?.Status != 0)
            {
                var errorMessage = $"Viber status {result?.Status}: {result?.StatusMessage}";

                _logger.LogDebug(
                    "Viber: API error for user {UserId}. {Error}",
                    payload.To, errorMessage);

                return Fail(payload, errorMessage);
            }

            var messageToken = result.MessageToken?.ToString();

            _logger.LogDebug(
                "Viber: message sent to user {UserId}. MessageToken {MessageToken}",
                payload.To, messageToken);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "viber",
                ProviderId = messageToken,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Viber: exception sending to user {UserId}",
                payload.To);

            return Fail(payload, ex.Message);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private NotifyResult Fail(NotificationPayload payload, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "viber",
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

internal sealed class ViberSendRequest
{
    [JsonPropertyName("receiver")]
    public string Receiver { get; set; } = string.Empty;

    [JsonPropertyName("sender")]
    public ViberSender Sender { get; set; } = new();

    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

internal sealed class ViberSender
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }
}

internal sealed class ViberSendResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("status_message")]
    public string? StatusMessage { get; set; }

    [JsonPropertyName("message_token")]
    public long? MessageToken { get; set; }
}
