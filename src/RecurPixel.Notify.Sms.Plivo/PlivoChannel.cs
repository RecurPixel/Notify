using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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

namespace RecurPixel.Notify.Sms.Plivo;

/// <summary>
/// Notification channel adapter for Plivo SMS delivery.
/// Plivo has no native bulk API — bulk is handled by the base class loop.
/// </summary>
public sealed class PlivoChannel : NotificationChannelBase
{
    private readonly PlivoOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<PlivoChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "sms";

    /// <summary>
    /// Initialises a new instance of <see cref="PlivoChannel"/>.
    /// </summary>
    public PlivoChannel(
        IOptions<PlivoOptions> options,
        HttpClient http,
        ILogger<PlivoChannel> logger)
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
            "Plivo: sending SMS to {To}",
            payload.To);

        try
        {
            var url = $"https://api.plivo.com/v1/Account/{_options.AuthId}/Message/";

            var body = new PlivoSendRequest
            {
                Src = _options.FromNumber,
                Dst = payload.To,
                Text = payload.Body ?? string.Empty
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_options.AuthId}:{_options.AuthToken}"));
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await _http.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Plivo: send failed for {To}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload.To, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<PlivoSendResponse>(raw, JsonOptions);
            var messageId = result?.MessageUuid?.FirstOrDefault();

            _logger.LogDebug(
                "Plivo: SMS sent to {To}. MessageId {MessageId}",
                payload.To, messageId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "plivo",
                ProviderId = messageId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Plivo: exception sending to {To}",
                payload.To);

            return Fail(payload.To, ex.Message);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private NotifyResult Fail(string to, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "plivo",
        Recipient = to,
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

internal sealed class PlivoSendRequest
{
    [JsonPropertyName("src")]
    public string Src { get; set; } = string.Empty;

    [JsonPropertyName("dst")]
    public string Dst { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

internal sealed class PlivoSendResponse
{
    [JsonPropertyName("message_uuid")]
    public string[]? MessageUuid { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
