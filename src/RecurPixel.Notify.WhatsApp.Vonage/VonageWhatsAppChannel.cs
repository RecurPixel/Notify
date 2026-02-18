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
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.WhatsApp.Vonage;

/// <summary>
/// Notification channel adapter for Vonage WhatsApp Business messaging.
/// Uses the Vonage Messages API to deliver WhatsApp messages.
/// Bulk is handled by the base class loop — Meta policy restricts bulk WhatsApp.
/// </summary>
public sealed class VonageWhatsAppChannel : NotificationChannelBase
{
    private readonly VonageWhatsAppOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<VonageWhatsAppChannel> _logger;

    private const string SendEndpoint = "https://messages-sandbox.nexmo.com/v1/messages";

    /// <inheritdoc />
    public override string ChannelName => "whatsapp";

    /// <summary>
    /// Initialises a new instance of <see cref="VonageWhatsAppChannel"/>.
    /// </summary>
    public VonageWhatsAppChannel(
        IOptions<VonageWhatsAppOptions> options,
        HttpClient http,
        ILogger<VonageWhatsAppChannel> logger)
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
            "Vonage WhatsApp: sending message to {To}",
            payload.To);

        try
        {
            var body = new VonageWhatsAppRequest
            {
                From = new VonageEndpoint { Type = "whatsapp", Number = _options.FromNumber },
                To = new VonageEndpoint { Type = "whatsapp", Number = payload.To },
                MessageType = "text",
                Text = new VonageTextContent
                {
                    Body = string.IsNullOrWhiteSpace(payload.Subject)
                               ? payload.Body ?? string.Empty
                               : $"{payload.Subject}\n\n{payload.Body}"
                },
                Channel = "whatsapp"
            };

            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_options.ApiKey}:{_options.ApiSecret}"));

            using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await _http.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Vonage WhatsApp: send failed for {To}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload.To, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<VonageWhatsAppResponse>(raw, JsonOptions);
            var messageId = result?.MessageUuid;

            _logger.LogDebug(
                "Vonage WhatsApp: message sent to {To}. MessageId {MessageId}",
                payload.To, messageId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "vonage",
                ProviderId = messageId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Vonage WhatsApp: exception sending to {To}",
                payload.To);

            return Fail(payload.To, ex.Message);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private NotifyResult Fail(string to, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "vonage",
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

internal sealed class VonageWhatsAppRequest
{
    [JsonPropertyName("from")]
    public VonageEndpoint From { get; set; } = new();

    [JsonPropertyName("to")]
    public VonageEndpoint To { get; set; } = new();

    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = "text";

    [JsonPropertyName("text")]
    public VonageTextContent? Text { get; set; }

    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "whatsapp";
}

internal sealed class VonageEndpoint
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;
}

internal sealed class VonageTextContent
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

internal sealed class VonageWhatsAppResponse
{
    [JsonPropertyName("message_uuid")]
    public string? MessageUuid { get; set; }
}
