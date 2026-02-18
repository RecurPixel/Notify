using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.WhatsApp.MetaCloud;

// ── Internal HTTP client abstraction ─────────────────────────────────────────

/// <summary>Result of a Meta Cloud API send attempt.</summary>
internal sealed class MetaCloudSendResult
{
    public bool   IsSuccess { get; init; }
    public string? MessageId { get; init; }
    public string? Error    { get; init; }
}

/// <summary>
/// Abstracts the Meta Cloud API HTTP call for testability.
/// </summary>
internal interface IMetaCloudClient
{
    Task<MetaCloudSendResult> SendAsync(
        string to,
        string body,
        CancellationToken ct);
}

/// <summary>
/// Real <see cref="IMetaCloudClient"/> — calls the Meta WhatsApp Cloud API.
/// POST https://graph.facebook.com/v17.0/{phoneNumberId}/messages
/// </summary>
internal sealed class MetaCloudHttpClient : IMetaCloudClient
{
    private readonly HttpClient _http;
    private readonly MetaCloudOptions _options;

    public MetaCloudHttpClient(HttpClient http, MetaCloudOptions options)
    {
        _http    = http;
        _options = options;
    }

    public async Task<MetaCloudSendResult> SendAsync(
        string to,
        string body,
        CancellationToken ct)
    {
        var url = $"https://graph.facebook.com/v17.0/{_options.PhoneNumberId}/messages";

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {_options.AccessToken}");

        request.Content = JsonContent.Create(new
        {
            messaging_product = "whatsapp",
            to,
            type = "text",
            text = new { body }
        });

        var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            var json      = await response.Content.ReadFromJsonAsync<MetaSuccessResponse>(ct);
            var messageId = json?.Messages?[0]?.Id;
            return new MetaCloudSendResult { IsSuccess = true, MessageId = messageId };
        }

        var error = await response.Content.ReadAsStringAsync(ct);
        return new MetaCloudSendResult
        {
            IsSuccess = false,
            Error     = $"Meta Cloud API returned {response.StatusCode}: {error}"
        };
    }

    // Minimal response shapes — only fields we need
    private sealed class MetaSuccessResponse
    {
        [JsonPropertyName("messages")]
        public MetaMessage[]? Messages { get; set; }
    }

    private sealed class MetaMessage
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}

// ── Channel ───────────────────────────────────────────────────────────────────

/// <summary>
/// WhatsApp channel adapter using the Meta WhatsApp Cloud API.
/// No native bulk API — bulk is handled automatically by the base class loop.
/// </summary>
public sealed class MetaCloudWhatsAppChannel : NotificationChannelBase
{
    private readonly MetaCloudOptions _options;
    private readonly IMetaCloudClient _client;
    private readonly ILogger<MetaCloudWhatsAppChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "whatsapp";

    /// <summary>DI constructor — uses the real Meta Cloud HTTP client.</summary>
    public MetaCloudWhatsAppChannel(
        IOptions<MetaCloudOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<MetaCloudWhatsAppChannel> logger)
        : this(
            options,
            new MetaCloudHttpClient(
                httpClientFactory.CreateClient(nameof(MetaCloudWhatsAppChannel)),
                options.Value),
            logger)
    { }

    /// <summary>Internal constructor — accepts a mock client for testing.</summary>
    internal MetaCloudWhatsAppChannel(
        IOptions<MetaCloudOptions> options,
        IMetaCloudClient client,
        ILogger<MetaCloudWhatsAppChannel> logger)
    {
        _options = options.Value;
        _client  = client;
        _logger  = logger;
    }

    /// <inheritdoc />
    public override async Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Meta Cloud WhatsApp: attempting send to {Recipient}", payload.To);

        try
        {
            var result = await _client.SendAsync(payload.To, payload.Body, ct);

            if (result.IsSuccess)
            {
                _logger.LogDebug(
                    "Meta Cloud WhatsApp: send succeeded to {Recipient} messageId={MessageId}",
                    payload.To, result.MessageId);

                return new NotifyResult
                {
                    Success    = true,
                    Channel    = ChannelName,
                    Provider   = "metacloud",
                    ProviderId = result.MessageId,
                    Recipient  = payload.To,
                    SentAt     = DateTime.UtcNow
                };
            }

            _logger.LogDebug(
                "Meta Cloud WhatsApp: send failed for {Recipient} error={Error}",
                payload.To, result.Error);

            return new NotifyResult
            {
                Success   = false,
                Channel   = ChannelName,
                Provider  = "metacloud",
                Recipient = payload.To,
                Error     = result.Error,
                SentAt    = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Meta Cloud WhatsApp: send threw for {Recipient}", payload.To);

            return new NotifyResult
            {
                Success   = false,
                Channel   = ChannelName,
                Provider  = "metacloud",
                Recipient = payload.To,
                Error     = ex.Message,
                SentAt    = DateTime.UtcNow
            };
        }
    }

    // No SendBulkAsync override — Meta policy restricts bulk WhatsApp.
    // The base class loop handles bulk automatically.
}
