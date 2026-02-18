using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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

namespace RecurPixel.Notify.Email.Mailgun;

/// <summary>
/// Notification channel adapter for Mailgun email delivery.
/// Supports native batch sending via Mailgun's recipient variables API.
/// </summary>
public sealed class MailgunChannel : NotificationChannelBase
{
    private readonly MailgunOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<MailgunChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "email";

    /// <summary>
    /// Initialises a new instance of <see cref="MailgunChannel"/>.
    /// </summary>
    public MailgunChannel(
        IOptions<MailgunOptions> options,
        HttpClient http,
        ILogger<MailgunChannel> logger)
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
            "Mailgun: sending email to {To}",
            payload.To);

        try
        {
            var form = BuildForm(
                new[] { payload.To },
                payload.Subject,
                payload.Body,
                recipientVariables: null);

            var response = await PostAsync(form, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Mailgun: send failed for {To}. Status {Status}. Body {Body}",
                    payload.To, (int)response.StatusCode, raw);

                return Fail(payload.To, $"HTTP {(int)response.StatusCode}: {raw}");
            }

            var result = JsonSerializer.Deserialize<MailgunSendResponse>(raw, JsonOptions);
            var messageId = result?.Id;

            _logger.LogDebug(
                "Mailgun: email sent to {To}. MessageId {MessageId}",
                payload.To, messageId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "mailgun",
                ProviderId = messageId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Mailgun: exception sending to {To}",
                payload.To);

            return Fail(payload.To, ex.Message);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses Mailgun's recipient variables API to send to up to 1000 recipients per call.
    /// Payloads are chunked automatically if they exceed <see cref="BulkOptions.MaxBatchSize"/>.
    /// </remarks>
    public override async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Mailgun: bulk send to {Count} recipients",
            payloads.Count);

        var allResults = new List<NotifyResult>();
        var chunks = payloads.Chunk(1000);

        foreach (var chunk in chunks)
        {
            var chunkList = chunk.ToList();

            try
            {
                var recipients = chunkList.Select(p => p.To).ToArray();
                var recipientVariables = chunkList.ToDictionary(
                    p => p.To,
                    p => new { subject = p.Subject ?? string.Empty });

                var rvJson = JsonSerializer.Serialize(recipientVariables);

                // Use first payload subject/body as the template —
                // recipient variables allow per-recipient overrides if needed
                var first = chunkList[0];
                var form = BuildForm(recipients, first.Subject, first.Body, rvJson);

                var response = await PostAsync(form, ct);
                var raw = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug(
                        "Mailgun: bulk chunk failed. Status {Status}. Body {Body}",
                        (int)response.StatusCode, raw);

                    allResults.AddRange(chunkList.Select(p =>
                        Fail(p.To, $"HTTP {(int)response.StatusCode}: {raw}")));
                }
                else
                {
                    var result = JsonSerializer.Deserialize<MailgunSendResponse>(raw, JsonOptions);
                    var messageId = result?.Id;

                    allResults.AddRange(chunkList.Select(p => new NotifyResult
                    {
                        Success = true,
                        Channel = ChannelName,
                        Provider = "mailgun",
                        ProviderId = messageId,
                        Recipient = p.To,
                        SentAt = DateTime.UtcNow
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Mailgun: exception in bulk chunk");
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

    private Task<HttpResponseMessage> PostAsync(
        MultipartFormDataContent form,
        CancellationToken ct)
    {
        var url = $"https://api.mailgun.net/v3/{_options.Domain}/messages";
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"api:{_options.ApiKey}"));
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        return _http.SendAsync(request, ct);
    }

    private MultipartFormDataContent BuildForm(
        string[] recipients,
        string? subject,
        string? body,
        string? recipientVariables)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent($"{_options.FromName} <{_options.FromEmail}>"), "from");
        form.Add(new StringContent(subject ?? string.Empty), "subject");

        // Detect HTML vs plain text
        if (body is not null && body.TrimStart().StartsWith("<"))
            form.Add(new StringContent(body), "html");
        else
            form.Add(new StringContent(body ?? string.Empty), "text");

        foreach (var to in recipients)
            form.Add(new StringContent(to), "to");

        if (recipientVariables is not null)
            form.Add(new StringContent(recipientVariables), "recipient-variables");

        return form;
    }

    private NotifyResult Fail(string to, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "mailgun",
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

// ── internal response shapes ─────────────────────────────────────────────────

internal sealed class MailgunSendResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
