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

namespace RecurPixel.Notify.Push.OneSignal;

/// <summary>
/// Notification channel adapter for OneSignal push notification delivery.
/// Supports native bulk sending via the OneSignal notifications API.
/// </summary>
public sealed class OneSignalChannel : NotificationChannelBase
{
    private readonly OneSignalOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<OneSignalChannel> _logger;

    private const string SendEndpoint = "https://onesignal.com/api/v1/notifications";

    /// <inheritdoc />
    public override string ChannelName => "push";

    /// <summary>
    /// Initialises a new instance of <see cref="OneSignalChannel"/>.
    /// </summary>
    public OneSignalChannel(
        IOptions<OneSignalOptions> options,
        HttpClient http,
        ILogger<OneSignalChannel> logger)
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
            "OneSignal: sending push to device {DeviceToken}",
            payload.To);

        try
        {
            var body = new OneSignalSendRequest
            {
                AppId = _options.AppId,
                IncludePlayerIds = new[] { payload.To },
                Headings = new OneSignalContent { En = payload.Subject ?? string.Empty },
                Contents = new OneSignalContent { En = payload.Body ?? string.Empty }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", _options.ApiKey);
            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await _http.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "OneSignal: send failed for device {DeviceToken}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload.To, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<OneSignalSendResponse>(raw, JsonOptions);
            var notificationId = result?.Id;

            _logger.LogDebug(
                "OneSignal: push sent to device {DeviceToken}. NotificationId {NotificationId}",
                payload.To, notificationId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "onesignal",
                ProviderId = notificationId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "OneSignal: exception sending to device {DeviceToken}",
                payload.To);

            return Fail(payload.To, ex.Message);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses OneSignal's include_player_ids field to send to multiple devices
    /// in a single API call. Chunks into batches of 2000 — the OneSignal limit.
    /// </remarks>
    public override async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "OneSignal: bulk send to {Count} devices",
            payloads.Count);

        var allResults = new List<NotifyResult>();
        var chunks = payloads.Chunk(2000);

        foreach (var chunk in chunks)
        {
            var chunkList = chunk.ToList();

            try
            {
                var first = chunkList[0];

                var body = new OneSignalSendRequest
                {
                    AppId = _options.AppId,
                    IncludePlayerIds = chunkList.Select(p => p.To).ToArray(),
                    Headings = new OneSignalContent { En = first.Subject ?? string.Empty },
                    Contents = new OneSignalContent { En = first.Body ?? string.Empty }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", _options.ApiKey);
                request.Content = JsonContent.Create(body, options: JsonOptions);

                var response = await _http.SendAsync(request, ct);
                var raw = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug(
                        "OneSignal: bulk chunk failed. Status {Status}. Body {Body}",
                        (int)response.StatusCode, raw);

                    allResults.AddRange(chunkList.Select(p =>
                        Fail(p.To, $"HTTP {(int)response.StatusCode}: {raw}")));
                    continue;
                }

                var result = JsonSerializer.Deserialize<OneSignalSendResponse>(raw, JsonOptions);
                var notificationId = result?.Id;

                allResults.AddRange(chunkList.Select(p => new NotifyResult
                {
                    Success = true,
                    Channel = ChannelName,
                    Provider = "onesignal",
                    ProviderId = notificationId,
                    Recipient = p.To,
                    SentAt = DateTime.UtcNow
                }));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "OneSignal: exception in bulk chunk");
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
        Provider = "onesignal",
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

internal sealed class OneSignalSendRequest
{
    [JsonPropertyName("app_id")]
    public string AppId { get; set; } = string.Empty;

    [JsonPropertyName("include_player_ids")]
    public string[] IncludePlayerIds { get; set; } = Array.Empty<string>();

    [JsonPropertyName("headings")]
    public OneSignalContent? Headings { get; set; }

    [JsonPropertyName("contents")]
    public OneSignalContent? Contents { get; set; }
}

internal sealed class OneSignalContent
{
    [JsonPropertyName("en")]
    public string En { get; set; } = string.Empty;
}

internal sealed class OneSignalSendResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("recipients")]
    public int Recipients { get; set; }

    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}
