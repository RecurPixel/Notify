using System.Net.Http.Json;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Discord;

/// <summary>
/// Notification channel adapter for Discord.
/// Delivers messages via Discord Incoming Webhooks.
/// Subject is rendered as a bold header prepended to the message content.
/// </summary>
public sealed class DiscordChannel : NotificationChannelBase
{
    private readonly DiscordOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DiscordChannel> _logger;

    /// <inheritdoc/>
    public override string ChannelName => "discord";

    /// <summary>
    /// Initialises a new instance of <see cref="DiscordChannel"/>.
    /// </summary>
    public DiscordChannel(
        IOptions<DiscordOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<DiscordChannel> logger)
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
            "Discord send attempt. To: {To} Subject: {Subject}",
            payload.To, payload.Subject);

        try
        {
            // Discord webhooks accept a simple JSON body with a "content" field.
            // Subject is prepended as bold markdown if present.
            var content = string.IsNullOrWhiteSpace(payload.Subject)
                ? payload.Body
                : $"**{payload.Subject}**\n{payload.Body}";

            var messageBody = new { content };

            using var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(_options.WebhookUrl, messageBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogDebug(
                    "Discord send failed. StatusCode: {StatusCode} Error: {Error}",
                    response.StatusCode, error);

                return Fail($"Discord webhook returned {(int)response.StatusCode}: {error}");
            }

            _logger.LogDebug("Discord send succeeded. To: {To}", payload.To);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "discord",
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Discord send threw an exception. To: {To}", payload.To);
            return Fail(ex.Message);
        }
    }

    private NotifyResult Fail(string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "discord",
        Recipient = string.Empty,
        Error = error,
        SentAt = DateTime.UtcNow
    };
}
