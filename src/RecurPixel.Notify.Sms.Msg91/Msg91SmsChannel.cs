using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify.Channels;

/// <summary>
/// Notification channel adapter for MSG91 SMS delivery.
/// Uses the MSG91 v2 Send SMS API. No native bulk — base class loop handles bulk.
/// </summary>
[ChannelAdapter("sms", "msg91")]
public sealed class Msg91SmsChannel : NotificationChannelBase
{
    private readonly Msg91SmsOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Msg91SmsChannel> _logger;

    private const string SendEndpoint = "https://api.msg91.com/api/v2/sendsms";

    /// <inheritdoc />
    public override string ChannelName => "sms";

    /// <summary>
    /// Initialises a new instance of <see cref="Msg91SmsChannel"/>.
    /// </summary>
    public Msg91SmsChannel(
        IOptions<Msg91SmsOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<Msg91SmsChannel> logger)
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
        _logger.LogDebug("MSG91 SMS: sending to {To}", payload.To);

        try
        {
            var body = new Msg91SmsRequest
            {
                Sender  = _options.SenderId,
                Route   = _options.Route,
                Country = "0",
                Sms     = new[]
                {
                    new Msg91SmsItem
                    {
                        Message = payload.Body ?? string.Empty,
                        To      = new[] { payload.To }
                    }
                }
            };

            var http = _httpClientFactory.CreateClient("sms:msg91");
            using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint);
            request.Headers.Add("authkey", _options.AuthKey);
            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await http.SendAsync(request, ct);
            var raw      = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "MSG91 SMS: send failed for {To}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload.To, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<Msg91SmsResponse>(raw, JsonOptions);

            if (result?.Type != "success")
            {
                var error = $"MSG91 error: {result?.Message}";
                _logger.LogDebug("MSG91 SMS: API error for {To}. {Error}", payload.To, error);
                return Fail(payload.To, error);
            }

            _logger.LogDebug(
                "MSG91 SMS: sent to {To}. RequestId {RequestId}",
                payload.To, result.Message);

            return new NotifyResult
            {
                Success     = true,
                Channel     = ChannelName,
                Provider    = "msg91",
                ProviderId  = result.Message,
                Recipient   = payload.To,
                SentAt      = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "MSG91 SMS: exception sending to {To}", payload.To);
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

internal sealed class Msg91SmsRequest
{
    [JsonPropertyName("sender")]
    public string Sender { get; set; } = string.Empty;

    [JsonPropertyName("route")]
    public string Route { get; set; } = "4";

    [JsonPropertyName("country")]
    public string Country { get; set; } = "0";

    [JsonPropertyName("sms")]
    public Msg91SmsItem[] Sms { get; set; } = Array.Empty<Msg91SmsItem>();
}

internal sealed class Msg91SmsItem
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string[] To { get; set; } = Array.Empty<string>();
}

internal sealed class Msg91SmsResponse
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
