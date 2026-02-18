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

namespace RecurPixel.Notify.Email.Resend;

/// <summary>
/// Notification channel adapter for Resend email delivery.
/// Resend has no native batch API — bulk is handled by the base class loop.
/// </summary>
public sealed class ResendChannel : NotificationChannelBase
{
    private readonly ResendOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<ResendChannel> _logger;

    private const string SendEndpoint = "https://api.resend.com/emails";

    /// <inheritdoc />
    public override string ChannelName => "email";

    /// <summary>
    /// Initialises a new instance of <see cref="ResendChannel"/>.
    /// </summary>
    public ResendChannel(
        IOptions<ResendOptions> options,
        HttpClient http,
        ILogger<ResendChannel> logger)
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
            "Resend: sending email to {To}",
            payload.To);

        try
        {
            var body = new ResendSendRequest
            {
                From = $"{_options.FromName} <{_options.FromEmail}>",
                To = new[] { payload.To },
                Subject = payload.Subject ?? string.Empty,
                Html = IsHtml(payload.Body) ? payload.Body : null,
                Text = IsHtml(payload.Body) ? null : payload.Body
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await _http.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Resend: send failed for {To}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload.To, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<ResendSendResponse>(raw, JsonOptions);
            var messageId = result?.Id;

            _logger.LogDebug(
                "Resend: email sent to {To}. MessageId {MessageId}",
                payload.To, messageId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "resend",
                ProviderId = messageId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Resend: exception sending to {To}",
                payload.To);

            return Fail(payload.To, ex.Message);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static bool IsHtml(string? body) =>
        body is not null && body.TrimStart().StartsWith("<");

    private NotifyResult Fail(string to, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "resend",
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

internal sealed class ResendSendRequest
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string[] To { get; set; } = Array.Empty<string>();

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("html")]
    public string? Html { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

internal sealed class ResendSendResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
