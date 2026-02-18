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

namespace RecurPixel.Notify.Email.Postmark;

/// <summary>
/// Notification channel adapter for Postmark email delivery.
/// Supports native batch sending via Postmark's batch messages endpoint.
/// </summary>
public sealed class PostmarkChannel : NotificationChannelBase
{
    private readonly PostmarkOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<PostmarkChannel> _logger;

    private const string SingleEndpoint = "https://api.postmarkapp.com/email";
    private const string BatchEndpoint = "https://api.postmarkapp.com/email/batch";

    /// <inheritdoc />
    public override string ChannelName => "email";

    /// <summary>
    /// Initialises a new instance of <see cref="PostmarkChannel"/>.
    /// </summary>
    public PostmarkChannel(
        IOptions<PostmarkOptions> options,
        HttpClient http,
        ILogger<PostmarkChannel> logger)
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
            "Postmark: sending email to {To}",
            payload.To);

        try
        {
            var body = BuildMessage(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, SingleEndpoint);
            AddHeaders(request);
            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await _http.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Postmark: send failed for {To}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload.To, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<PostmarkSendResponse>(raw, JsonOptions);
            var messageId = result?.MessageId;

            _logger.LogDebug(
                "Postmark: email sent to {To}. MessageId {MessageId}",
                payload.To, messageId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "postmark",
                ProviderId = messageId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Postmark: exception sending to {To}",
                payload.To);

            return Fail(payload.To, ex.Message);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses Postmark's batch messages endpoint — up to 500 messages per call.
    /// Payloads are chunked automatically into batches of 500.
    /// </remarks>
    public override async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Postmark: bulk send to {Count} recipients",
            payloads.Count);

        var allResults = new List<NotifyResult>();
        var chunks = payloads.Chunk(500);

        foreach (var chunk in chunks)
        {
            var chunkList = chunk.ToList();

            try
            {
                var messages = chunkList.Select(BuildMessage).ToList();

                using var request = new HttpRequestMessage(HttpMethod.Post, BatchEndpoint);
                AddHeaders(request);
                request.Content = JsonContent.Create(messages, options: JsonOptions);

                var response = await _http.SendAsync(request, ct);
                var raw = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug(
                        "Postmark: bulk chunk failed. Status {Status}. Body {Body}",
                        (int)response.StatusCode, raw);

                    allResults.AddRange(chunkList.Select(p =>
                        Fail(p.To, $"HTTP {(int)response.StatusCode}: {raw}")));
                    continue;
                }

                var responses = JsonSerializer.Deserialize<List<PostmarkSendResponse>>(
                    raw, JsonOptions) ?? new List<PostmarkSendResponse>();

                for (var i = 0; i < chunkList.Count; i++)
                {
                    var payload = chunkList[i];
                    var r = i < responses.Count ? responses[i] : null;
                    var errorCode = r?.ErrorCode ?? 0;

                    if (errorCode == 0)
                    {
                        allResults.Add(new NotifyResult
                        {
                            Success = true,
                            Channel = ChannelName,
                            Provider = "postmark",
                            ProviderId = r?.MessageId,
                            Recipient = payload.To,
                            SentAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        allResults.Add(Fail(payload.To,
                            $"Postmark error {errorCode}: {r?.Message}"));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Postmark: exception in bulk chunk");
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

    private PostmarkMessage BuildMessage(NotificationPayload payload)
    {
        var isHtml = payload.Body is not null &&
                     payload.Body.TrimStart().StartsWith("<");
        return new PostmarkMessage
        {
            From = $"{_options.FromName} <{_options.FromEmail}>",
            To = payload.To,
            Subject = payload.Subject ?? string.Empty,
            HtmlBody = isHtml ? payload.Body : null,
            TextBody = isHtml ? null : payload.Body
        };
    }

    private void AddHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-Postmark-Server-Token", _options.ApiKey);
        request.Headers.Add("Accept", "application/json");
    }

    private NotifyResult Fail(string to, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "postmark",
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

internal sealed class PostmarkMessage
{
    [JsonPropertyName("From")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("To")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("Subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("HtmlBody")]
    public string? HtmlBody { get; set; }

    [JsonPropertyName("TextBody")]
    public string? TextBody { get; set; }
}

internal sealed class PostmarkSendResponse
{
    [JsonPropertyName("MessageID")]
    public string? MessageId { get; set; }

    [JsonPropertyName("ErrorCode")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("Message")]
    public string? Message { get; set; }
}
