using System;
using System.Net.Http;
using System.Net.Http.Headers;
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

namespace RecurPixel.Notify.Sms.MessageBird;

/// <summary>
/// Notification channel adapter for MessageBird SMS delivery.
/// MessageBird has no native bulk API — bulk is handled by the base class loop.
/// </summary>
public sealed class MessageBirdChannel : NotificationChannelBase
{
    private readonly MessageBirdOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<MessageBirdChannel> _logger;

    private const string SendEndpoint = "https://rest.messagebird.com/messages";

    /// <inheritdoc />
    public override string ChannelName => "sms";

    /// <summary>
    /// Initialises a new instance of <see cref="MessageBirdChannel"/>.
    /// </summary>
    public MessageBirdChannel(
        IOptions<MessageBirdOptions> options,
        HttpClient http,
        ILogger<MessageBirdChannel> logger)
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
            "MessageBird: sending SMS to {To}",
            payload.To);

        try
        {
            var body = new MessageBirdSendRequest
            {
                Originator = _options.Originator,
                Recipients = new[] { payload.To },
                Body = payload.Body ?? string.Empty
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("AccessKey", _options.ApiKey);
            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await _http.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "MessageBird: send failed for {To}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload.To, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<MessageBirdSendResponse>(raw, JsonOptions);
            var messageId = result?.Id;

            _logger.LogDebug(
                "MessageBird: SMS sent to {To}. MessageId {MessageId}",
                payload.To, messageId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "messagebird",
                ProviderId = messageId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "MessageBird: exception sending to {To}",
                payload.To);

            return Fail(payload.To, ex.Message);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private NotifyResult Fail(string to, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "messagebird",
        Recipient = to,
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

internal sealed class MessageBirdSendRequest
{
    [JsonPropertyName("originator")]
    public string Originator { get; set; } = string.Empty;

    [JsonPropertyName("recipients")]
    public string[] Recipients { get; set; } = Array.Empty<string>();

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

internal sealed class MessageBirdSendResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }
}
