using System;
using System.Collections.Generic;
using System.Linq;
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

namespace RecurPixel.Notify.Sms.Sinch;

/// <summary>
/// Notification channel adapter for Sinch SMS delivery.
/// Supports native batch sending via the Sinch Batch SMS API.
/// </summary>
public sealed class SinchChannel : NotificationChannelBase
{
    private readonly SinchOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<SinchChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "sms";

    /// <summary>
    /// Initialises a new instance of <see cref="SinchChannel"/>.
    /// </summary>
    public SinchChannel(
        IOptions<SinchOptions> options,
        HttpClient http,
        ILogger<SinchChannel> logger)
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
            "Sinch: sending SMS to {To}",
            payload.To);

        try
        {
            var url = $"https://us.sms.api.sinch.com/xms/v1/{_options.ServicePlanId}/batches";

            var body = new SinchSendRequest
            {
                From = _options.FromNumber,
                To = new[] { payload.To },
                Body = payload.Body ?? string.Empty
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiToken);
            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await _http.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Sinch: send failed for {To}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload.To, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<SinchSendResponse>(raw, JsonOptions);
            var messageId = result?.Id;

            _logger.LogDebug(
                "Sinch: SMS sent to {To}. MessageId {MessageId}",
                payload.To, messageId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "sinch",
                ProviderId = messageId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Sinch: exception sending to {To}",
                payload.To);

            return Fail(payload.To, ex.Message);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses Sinch Batch SMS API — sends to multiple recipients in a single call.
    /// Payloads are chunked into batches of 1000 — the Sinch batch API limit.
    /// </remarks>
    public override async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Sinch: bulk send to {Count} recipients",
            payloads.Count);

        var allResults = new List<NotifyResult>();
        var chunks = payloads.Chunk(1000);
        var url = $"https://us.sms.api.sinch.com/xms/v1/{_options.ServicePlanId}/batches";

        foreach (var chunk in chunks)
        {
            var chunkList = chunk.ToList();

            try
            {
                // Sinch batch API supports one body for all recipients in the batch
                // Use the first payload body — for distinct bodies, base loop is preferred
                var body = new SinchSendRequest
                {
                    From = _options.FromNumber,
                    To = chunkList.Select(p => p.To).ToArray(),
                    Body = chunkList[0].Body ?? string.Empty
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _options.ApiToken);
                request.Content = JsonContent.Create(body, options: JsonOptions);

                var response = await _http.SendAsync(request, ct);
                var raw = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug(
                        "Sinch: bulk chunk failed. Status {Status}. Body {Body}",
                        (int)response.StatusCode, raw);

                    allResults.AddRange(chunkList.Select(p =>
                        Fail(p.To, $"HTTP {(int)response.StatusCode}: {raw}")));
                    continue;
                }

                var result = JsonSerializer.Deserialize<SinchSendResponse>(raw, JsonOptions);
                var messageId = result?.Id;

                allResults.AddRange(chunkList.Select(p => new NotifyResult
                {
                    Success = true,
                    Channel = ChannelName,
                    Provider = "sinch",
                    ProviderId = messageId,
                    Recipient = p.To,
                    SentAt = DateTime.UtcNow
                }));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Sinch: exception in bulk chunk");
                allResults.AddRange(chunkList.Select(p => Fail(p.To, ex.Message)));
            }
        }

        return new BulkNotifyResult
        {
            Results = allResults,
            Channel = ChannelName,
            UsedNativeBatch = true
        };
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private NotifyResult Fail(string to, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "sinch",
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

internal sealed class SinchSendRequest
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string[] To { get; set; } = Array.Empty<string>();

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

internal sealed class SinchSendResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
