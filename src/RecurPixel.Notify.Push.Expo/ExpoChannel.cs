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

namespace RecurPixel.Notify.Push.Expo;

/// <summary>
/// Notification channel adapter for Expo push notification delivery.
/// Supports native batch sending via the Expo push tickets API.
/// </summary>
public sealed class ExpoChannel : NotificationChannelBase
{
    private readonly ExpoOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<ExpoChannel> _logger;

    private const string SendEndpoint = "https://exp.host/--/api/v2/push/send";

    /// <inheritdoc />
    public override string ChannelName => "push";

    /// <summary>
    /// Initialises a new instance of <see cref="ExpoChannel"/>.
    /// </summary>
    public ExpoChannel(
        IOptions<ExpoOptions> options,
        HttpClient http,
        ILogger<ExpoChannel> logger)
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
            "Expo: sending push to token {DeviceToken}",
            payload.To);

        try
        {
            var message = BuildMessage(payload);

            using var request = BuildRequest(new[] { message });
            var response = await _http.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Expo: send failed for token {DeviceToken}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload.To, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<ExpoPushResponse>(raw, JsonOptions);
            var ticket = result?.Data?.FirstOrDefault();

            if (ticket?.Status == "error")
            {
                var error = $"Expo error {ticket.Details?.Error}: {ticket.Message}";
                _logger.LogDebug(
                    "Expo: push ticket error for token {DeviceToken}. {Error}",
                    payload.To, error);

                return Fail(payload.To, error);
            }

            var ticketId = ticket?.Id;

            _logger.LogDebug(
                "Expo: push sent to token {DeviceToken}. TicketId {TicketId}",
                payload.To, ticketId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "expo",
                ProviderId = ticketId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Expo: exception sending to token {DeviceToken}",
                payload.To);

            return Fail(payload.To, ex.Message);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses Expo's push tickets batch API — sends up to 100 messages per call.
    /// Payloads are chunked into batches of 100 — the Expo recommended limit.
    /// </remarks>
    public override async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Expo: bulk send to {Count} tokens",
            payloads.Count);

        var allResults = new List<NotifyResult>();
        var chunks = payloads.Chunk(100);

        foreach (var chunk in chunks)
        {
            var chunkList = chunk.ToList();

            try
            {
                var messages = chunkList.Select(BuildMessage).ToList();

                using var request = BuildRequest(messages);
                var response = await _http.SendAsync(request, ct);
                var raw = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug(
                        "Expo: bulk chunk failed. Status {Status}. Body {Body}",
                        (int)response.StatusCode, raw);

                    allResults.AddRange(chunkList.Select(p =>
                        Fail(p.To, $"HTTP {(int)response.StatusCode}: {raw}")));
                    continue;
                }

                var result = JsonSerializer.Deserialize<ExpoPushResponse>(raw, JsonOptions);
                var tickets = result?.Data ?? new List<ExpoPushTicket>();

                for (var i = 0; i < chunkList.Count; i++)
                {
                    var payload = chunkList[i];
                    var ticket = i < tickets.Count ? tickets[i] : null;

                    if (ticket?.Status == "error")
                    {
                        allResults.Add(Fail(payload.To,
                            $"Expo error {ticket.Details?.Error}: {ticket.Message}"));
                    }
                    else
                    {
                        allResults.Add(new NotifyResult
                        {
                            Success = true,
                            Channel = ChannelName,
                            Provider = "expo",
                            ProviderId = ticket?.Id,
                            Recipient = payload.To,
                            SentAt = DateTime.UtcNow
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Expo: exception in bulk chunk");
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

    private static ExpoPushMessage BuildMessage(NotificationPayload payload) =>
        new()
        {
            To = payload.To,
            Title = payload.Subject,
            Body = payload.Body ?? string.Empty
        };

    private HttpRequestMessage BuildRequest(IEnumerable<ExpoPushMessage> messages)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint);

        if (!string.IsNullOrEmpty(_options.AccessToken))
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        request.Content = JsonContent.Create(messages, options: JsonOptions);
        return request;
    }

    private NotifyResult Fail(string to, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "expo",
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

internal sealed class ExpoPushMessage
{
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

internal sealed class ExpoPushResponse
{
    [JsonPropertyName("data")]
    public List<ExpoPushTicket>? Data { get; set; }
}

internal sealed class ExpoPushTicket
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("details")]
    public ExpoPushTicketDetails? Details { get; set; }
}

internal sealed class ExpoPushTicketDetails
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
