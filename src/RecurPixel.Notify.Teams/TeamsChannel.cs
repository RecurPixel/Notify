using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Teams;

/// <summary>
/// Notification channel adapter for Microsoft Teams.
/// Delivers messages via Teams Incoming Webhooks using a simple MessageCard payload.
/// Subject maps to the card title; Body maps to the card text.
/// </summary>
public sealed class TeamsChannel : NotificationChannelBase
{
    private readonly TeamsOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TeamsChannel> _logger;

    /// <inheritdoc/>
    public override string ChannelName => "teams";

    /// <summary>
    /// Initialises a new instance of <see cref="TeamsChannel"/>.
    /// </summary>
    public TeamsChannel(
        IOptions<TeamsOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<TeamsChannel> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Teams send attempt. To: {To} Subject: {Subject}",
            payload.To, payload.Subject);

        try
        {
            // Teams Incoming Webhooks accept a MessageCard JSON payload.
            // @type and @context are required fields.
            // themeColor is an optional accent — using RecurPixel brand blue.
            var messageBody = new
            {
                type = "MessageCard",
                context = "https://schema.org/extensions",
                themeColor = "0078D4",
                summary = payload.Subject ?? payload.Body,
                title = payload.Subject ?? string.Empty,
                text = payload.Body
            };

            using var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(_options.WebhookUrl, messageBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogDebug(
                    "Teams send failed. StatusCode: {StatusCode} Error: {Error}",
                    response.StatusCode, error);

                return Fail($"Teams webhook returned {(int)response.StatusCode}: {error}");
            }

            _logger.LogDebug("Teams send succeeded. To: {To}", payload.To);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "teams",
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Teams send threw an exception. To: {To}", payload.To);
            return Fail(ex.Message);
        }
    }

    private NotifyResult Fail(string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "teams",
        Recipient = string.Empty,
        Error = error,
        SentAt = DateTime.UtcNow
    };
}