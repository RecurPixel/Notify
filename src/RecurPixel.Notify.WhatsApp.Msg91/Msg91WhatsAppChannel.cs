using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify.Channels;

/// <summary>
/// Notification channel adapter for MSG91 WhatsApp Business messaging.
/// Uses the MSG91 WhatsApp outbound message API. No native bulk — Meta policy restricts bulk WhatsApp.
/// </summary>
[ChannelAdapter("whatsapp", "msg91")]
public sealed class Msg91WhatsAppChannel : NotificationChannelBase
{
    private readonly Msg91WhatsAppOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Msg91WhatsAppChannel> _logger;

    private const string SendEndpoint = "https://api.msg91.com/api/v5/whatsapp/whatsapp-outbound-message/";

    /// <inheritdoc />
    public override string ChannelName => "whatsapp";

    /// <summary>
    /// Initialises a new instance of <see cref="Msg91WhatsAppChannel"/>.
    /// </summary>
    public Msg91WhatsAppChannel(
        IOptions<Msg91WhatsAppOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<Msg91WhatsAppChannel> logger)
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
        _logger.LogDebug("MSG91 WhatsApp: sending to {To}", payload.To);

        try
        {
            var message = string.IsNullOrWhiteSpace(payload.Subject)
                ? payload.Body ?? string.Empty
                : $"{payload.Subject}\n\n{payload.Body}";

            var body = new Msg91WhatsAppRequest
            {
                IntegratedNumber = _options.IntegratedNumber,
                ContentType      = "text",
                Payload          = new Msg91WhatsAppPayload
                {
                    To   = payload.To,
                    Type = "text",
                    Text = new Msg91WhatsAppText { Body = message }
                }
            };

            var http = _httpClientFactory.CreateClient("whatsapp:msg91");
            using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint);
            request.Headers.Add("Authkey", _options.AuthKey);
            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await http.SendAsync(request, ct);
            var raw      = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "MSG91 WhatsApp: send failed for {To}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload.To, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<Msg91WhatsAppResponse>(raw, JsonOptions);

            if (result?.Type != "success")
            {
                var error = $"MSG91 error: {result?.Message}";
                _logger.LogDebug("MSG91 WhatsApp: API error for {To}. {Error}", payload.To, error);
                return Fail(payload.To, error);
            }

            _logger.LogDebug(
                "MSG91 WhatsApp: sent to {To}. RequestId {RequestId}",
                payload.To, result.Message);

            return new NotifyResult
            {
                Success    = true,
                Channel    = ChannelName,
                Provider   = "msg91",
                ProviderId = result.Message,
                Recipient  = payload.To,
                SentAt     = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "MSG91 WhatsApp: exception sending to {To}", payload.To);
            return Fail(payload.To, ex.Message);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private NotifyResult Fail(string to, string error) => new()
    {
        Success   = false,
        Channel   = ChannelName,
        Provider  = "msg91",
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

// ── internal request / response shapes ──────────────────────────────────────

internal sealed class Msg91WhatsAppRequest
{
    [JsonPropertyName("integrated_number")]
    public string IntegratedNumber { get; set; } = string.Empty;

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = "text";

    [JsonPropertyName("payload")]
    public Msg91WhatsAppPayload Payload { get; set; } = new();
}

internal sealed class Msg91WhatsAppPayload
{
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public Msg91WhatsAppText? Text { get; set; }
}

internal sealed class Msg91WhatsAppText
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

internal sealed class Msg91WhatsAppResponse
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
