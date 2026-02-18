using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;

namespace RecurPixel.Notify.Email.AzureCommEmail;

/// <summary>
/// Notification channel adapter for Azure Communication Services Email.
/// Bulk is handled by the base class loop — ACS Email has no native batch API.
/// </summary>
public sealed class AzureCommEmailChannel : NotificationChannelBase
{
    private readonly AzureCommEmailOptions _options;
    private readonly IAzureCommEmailClient _client;
    private readonly ILogger<AzureCommEmailChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "email";

    /// <summary>
    /// Initialises a new instance of <see cref="AzureCommEmailChannel"/>.
    /// </summary>
    public AzureCommEmailChannel(
        IOptions<AzureCommEmailOptions> options,
        IAzureCommEmailClient client,
        ILogger<AzureCommEmailChannel> logger)
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
            "AzureCommEmail: sending email to {To}",
            payload.To);

        try
        {
            var from = string.IsNullOrWhiteSpace(_options.FromName)
                ? _options.FromEmail
                : $"{_options.FromName} <{_options.FromEmail}>";

            var isHtml = IsHtml(payload.Body);

            var messageId = await _client.SendAsync(
                senderAddress:    from,
                recipientAddress: payload.To,
                subject:          payload.Subject ?? string.Empty,
                html:             isHtml ? payload.Body : null,
                plainText:        isHtml ? null : payload.Body,
                ct:               ct);

            _logger.LogDebug(
                "AzureCommEmail: email sent to {To}. MessageId {MessageId}",
                payload.To, messageId);

            return new NotifyResult
            {
                Success    = true,
                Channel    = ChannelName,
                Provider   = "azurecommemail",
                ProviderId = messageId,
                Recipient  = payload.To,
                SentAt     = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "AzureCommEmail: exception sending to {To}",
                payload.To);

            return Fail(payload.To, ex.Message);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static bool IsHtml(string? body) =>
        body is not null && body.TrimStart().StartsWith("<");

    private NotifyResult Fail(string to, string error) => new()
    {
        Success   = false,
        Channel   = ChannelName,
        Provider  = "azurecommemail",
        Recipient = to,
        Error     = error,
        SentAt    = DateTime.UtcNow
    };
}
