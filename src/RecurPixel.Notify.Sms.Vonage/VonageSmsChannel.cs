using System;
using System.Collections.Generic;
using System.Linq;
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

namespace RecurPixel.Notify.Sms.Vonage;

/// <summary>
/// Notification channel adapter for Vonage SMS delivery.
/// Supports native bulk sending via the Vonage SMS API.
/// </summary>
public sealed class VonageSmsChannel : NotificationChannelBase
{
    private readonly VonageOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<VonageSmsChannel> _logger;

    private const string SendEndpoint = "https://rest.nexmo.com/sms/json";

    /// <inheritdoc />
    public override string ChannelName => "sms";

    /// <summary>
    /// Initialises a new instance of <see cref="VonageSmsChannel"/>.
    /// </summary>
    public VonageSmsChannel(
        IOptions<VonageOptions> options,
        HttpClient http,
        ILogger<VonageSmsChannel> logger)
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
            "Vonage: sending SMS to {To}",
            payload.To);

        try
        {
            var body = new VonageSendRequest
            {
                ApiKey = _options.ApiKey,
                ApiSecret = _options.ApiSecret,
                From = _options.FromNumber,
                To = payload.To,
                Text = payload.Body ?? string.Empty
            };

            var response = await _http.PostAsJsonAsync(SendEndpoint, body, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Vonage: send failed for {To}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload.To, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<VonageSendResponse>(raw, JsonOptions);

            // Vonage returns per-message status in the messages array
            var first = result?.Messages?.FirstOrDefault();
            if (first is null || first.Status != "0")
            {
                var error = $"Vonage status {first?.Status}: {first?.ErrorText}";
                _logger.LogDebug(
                    "Vonage: API error for {To}. {Error}",
                    payload.To, error);

                return Fail(payload.To, error);
            }

            _logger.LogDebug(
                "Vonage: SMS sent to {To}. MessageId {MessageId}",
                payload.To, first.MessageId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "vonage",
                ProviderId = first.MessageId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Vonage: exception sending to {To}",
                payload.To);

            return Fail(payload.To, ex.Message);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Loops single sends with a concurrency cap — Vonage SMS API is
    /// per-recipient but parallelises well within rate limits.
    /// </remarks>
    public override async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Vonage: bulk send to {Count} recipients",
            payloads.Count);

        var semaphore = new SemaphoreSlim(BulkConcurrencyLimit);
        var tasks = payloads.Select(async payload =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var result = await SendAsync(payload, ct);
                result.Recipient = payload.To;
                return result;
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        return new BulkNotifyResult
        {
            Results = results,
            Channel = ChannelName,
            UsedNativeBatch = false
        };
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

internal sealed class VonageSendRequest
{
    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("api_secret")]
    public string ApiSecret { get; set; } = string.Empty;

    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

internal sealed class VonageSendResponse
{
    [JsonPropertyName("messages")]
    public List<VonageMessageResult>? Messages { get; set; }
}

internal sealed class VonageMessageResult
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message-id")]
    public string? MessageId { get; set; }

    [JsonPropertyName("error-text")]
    public string? ErrorText { get; set; }
}
