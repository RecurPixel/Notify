using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Slack;

/// <summary>
/// Notification channel adapter for Slack.
/// Delivers messages via Slack Incoming Webhooks.
/// Extend with Bot API (chat.postMessage) by setting <see cref="SlackOptions.BotToken"/>.
/// </summary>
public sealed class SlackChannel : NotificationChannelBase
{
    private readonly SlackOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SlackChannel> _logger;

    /// <inheritdoc/>
    public override string ChannelName => "slack";

    /// <summary>
    /// Initialises a new instance of <see cref="SlackChannel"/>.
    /// </summary>
    public SlackChannel(
        IOptions<SlackOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<SlackChannel> logger)
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
        var webhookUrl = _options.WebhookUrl;

        _logger.LogDebug(
            "Slack send attempt. To: {To} Subject: {Subject}",
            payload.To, payload.Subject);

        try
        {
            // Build Slack message body.
            // Subject maps to an optional bold header block; Body is the main text.
            object messageBody;

            if (!string.IsNullOrWhiteSpace(payload.Subject))
            {
                messageBody = new
                {
                    blocks = new object[]
                    {
                        new
                        {
                            type = "header",
                            text = new { type = "plain_text", text = payload.Subject, emoji = true }
                        },
                        new
                        {
                            type = "section",
                            text = new { type = "mrkdwn", text = payload.Body }
                        }
                    }
                };
            }
            else
            {
                messageBody = new { text = payload.Body };
            }

            using var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(webhookUrl, messageBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogDebug(
                    "Slack send failed. StatusCode: {StatusCode} Error: {Error}",
                    response.StatusCode, error);

                return Fail($"Slack webhook returned {(int)response.StatusCode}: {error}");
            }

            _logger.LogDebug("Slack send succeeded. To: {To}", payload.To);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "slack",
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Slack send threw an exception. To: {To}", payload.To);
            return Fail(ex.Message);
        }
    }

    private NotifyResult Fail(string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "slack",
        Recipient = string.Empty,
        Error = error,
        SentAt = DateTime.UtcNow
    };
}
