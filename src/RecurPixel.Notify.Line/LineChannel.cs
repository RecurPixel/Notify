using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;

namespace RecurPixel.Notify.Line;

/// <summary>
/// Notification channel adapter for the LINE Messaging API.
/// Sends push messages via POST https://api.line.me/v2/bot/message/push.
/// </summary>
public sealed class LineChannel : NotificationChannelBase
{
    private readonly LineOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<LineChannel> _logger;

    private const string PushEndpoint = "https://api.line.me/v2/bot/message/push";

    /// <inheritdoc />
    public override string ChannelName => "line";

    /// <summary>
    /// Initialises a new instance of <see cref="LineChannel"/>.
    /// </summary>
    public LineChannel(
        IOptions<LineOptions> options,
        HttpClient http,
        ILogger<LineChannel> logger)
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
            "LINE: sending message to user {UserId}",
            payload.To);

        try
        {
            var body = new LinePushRequest
            {
                To = payload.To,
                Messages = new[]
                {
                    new LineTextMessage
                    {
                        Text = string.IsNullOrWhiteSpace(payload.Subject)
                                   ? payload.Body
                                   : $"{payload.Subject}\n\n{payload.Body}"
                    }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, PushEndpoint);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ChannelAccessToken);
            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await _http.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "LINE: send failed for user {UserId}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            // LINE returns the request ID in the X-Line-Request-Id response header
            response.Headers.TryGetValues("X-Line-Request-Id", out var values);
            var requestId = values is not null
                ? string.Join(",", values)
                : null;

            _logger.LogDebug(
                "LINE: message sent to user {UserId}. RequestId {RequestId}",
                payload.To, requestId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "line",
                ProviderId = requestId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "LINE: exception sending to user {UserId}",
                payload.To);

            return Fail(payload, ex.Message);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private NotifyResult Fail(NotificationPayload payload, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "line",
        Recipient = payload.To,
        Error = error,
        SentAt = DateTime.UtcNow
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };
}

// ── internal request / response shapes ──────────────────────────────────────

internal sealed class LinePushRequest
{
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public LineTextMessage[] Messages { get; set; } = Array.Empty<LineTextMessage>();
}

internal sealed class LineTextMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
