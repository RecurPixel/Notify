using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Email.Smtp;

/// <summary>
/// Email channel adapter for SMTP.
/// No native bulk API — bulk is handled automatically by the base class loop.
/// </summary>
public sealed class SmtpChannel : NotificationChannelBase
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "email";

    /// <summary>
    /// Initialises a new instance of <see cref="SmtpChannel"/>.
    /// </summary>
    public SmtpChannel(
        IOptions<SmtpOptions> options,
        ILogger<SmtpChannel> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    /// <inheritdoc />
    public override async Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Smtp: attempting send to {Recipient}", payload.To);

        try
        {
            using var client = new System.Net.Mail.SmtpClient(_options.Host, _options.Port)
            {
                Credentials = new NetworkCredential(_options.Username, _options.Password),
                EnableSsl   = _options.UseSsl
            };

            var from    = new MailAddress(_options.FromEmail, _options.FromName);
            var to      = new MailAddress(payload.To);
            var message = new MailMessage(from, to)
            {
                Subject    = payload.Subject,
                Body       = payload.Body,
                IsBodyHtml = true
            };

            await client.SendMailAsync(message, ct);

            _logger.LogDebug("Smtp: send succeeded to {Recipient}", payload.To);

            return new NotifyResult
            {
                Success   = true,
                Channel   = ChannelName,
                Provider  = "smtp",
                Recipient = payload.To,
                SentAt    = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Smtp: send failed for {Recipient}", payload.To);

            return new NotifyResult
            {
                Success   = false,
                Channel   = ChannelName,
                Provider  = "smtp",
                Recipient = payload.To,
                Error     = ex.Message,
                SentAt    = DateTime.UtcNow
            };
        }
    }

    // No SendBulkAsync override — SMTP is a single-send protocol.
    // The base class loop handles bulk automatically.
}
