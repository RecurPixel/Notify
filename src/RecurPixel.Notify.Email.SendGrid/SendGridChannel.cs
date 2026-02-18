using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace RecurPixel.Notify.Email.SendGrid;

/// <summary>
/// Email channel adapter for Twilio SendGrid.
/// Supports both single send and native batch send (up to 1000 recipients per call).
/// </summary>
public sealed class SendGridChannel : NotificationChannelBase
{
    private readonly SendGridOptions _options;
    private readonly ILogger<SendGridChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "email";

    /// <summary>
    /// Initialises a new instance of <see cref="SendGridChannel"/>.
    /// </summary>
    public SendGridChannel(
        IOptions<SendGridOptions> options,
        ILogger<SendGridChannel> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    /// <inheritdoc />
    public override async Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        _logger.LogDebug("SendGrid: attempting single send to {Recipient}", payload.To);

        try
        {
            var client  = new SendGridClient(_options.ApiKey);
            var from    = new EmailAddress(_options.FromEmail, _options.FromName);
            var to      = new EmailAddress(payload.To);
            var message = MailHelper.CreateSingleEmail(
                from,
                to,
                payload.Subject,
                plainTextContent: null,
                htmlContent: payload.Body);

            var response = await client.SendEmailAsync(message, ct);

            if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
            {
                var messageId = response.Headers
                    .FirstOrDefault(h => h.Key == "X-Message-Id").Value?
                    .FirstOrDefault();

                _logger.LogDebug(
                    "SendGrid: single send succeeded to {Recipient} messageId={MessageId}",
                    payload.To, messageId);

                return new NotifyResult
                {
                    Success    = true,
                    Channel    = ChannelName,
                    Provider   = "sendgrid",
                    ProviderId = messageId,
                    Recipient  = payload.To,
                    SentAt     = DateTime.UtcNow
                };
            }

            var body  = await response.Body.ReadAsStringAsync();
            var error = $"SendGrid returned {response.StatusCode}: {body}";

            _logger.LogDebug(
                "SendGrid: single send failed for {Recipient} status={Status}",
                payload.To, response.StatusCode);

            return new NotifyResult
            {
                Success   = false,
                Channel   = ChannelName,
                Provider  = "sendgrid",
                Recipient = payload.To,
                Error     = error,
                SentAt    = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "SendGrid: single send threw for {Recipient}", payload.To);

            return new NotifyResult
            {
                Success   = false,
                Channel   = ChannelName,
                Provider  = "sendgrid",
                Recipient = payload.To,
                Error     = ex.Message,
                SentAt    = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Sends to multiple recipients using SendGrid's native batch endpoint.
    /// Automatically chunks payloads into batches of up to 1000.
    /// </summary>
    public override async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "SendGrid: attempting bulk send for {Count} recipients", payloads.Count);

        var allResults = new List<NotifyResult>();
        var client     = new SendGridClient(_options.ApiKey);
        var from       = new EmailAddress(_options.FromEmail, _options.FromName);
        var chunks     = payloads.Chunk(1000);

        foreach (var chunk in chunks)
        {
            var personalizations = chunk.Select(p => new Personalization
            {
                Tos     = new List<EmailAddress> { new EmailAddress(p.To) },
                Subject = p.Subject
            }).ToList();

            var message = new SendGridMessage
            {
                From             = from,
                Personalizations = personalizations,
                HtmlContent      = chunk.First().Body
            };

            try
            {
                var response = await client.SendEmailAsync(message, ct);

                if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
                {
                    var messageId = response.Headers
                        .FirstOrDefault(h => h.Key == "X-Message-Id").Value?
                        .FirstOrDefault();

                    _logger.LogDebug(
                        "SendGrid: bulk chunk of {Count} succeeded messageId={MessageId}",
                        chunk.Length, messageId);

                    foreach (var payload in chunk)
                    {
                        allResults.Add(new NotifyResult
                        {
                            Success    = true,
                            Channel    = ChannelName,
                            Provider   = "sendgrid",
                            ProviderId = messageId,
                            Recipient  = payload.To,
                            SentAt     = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    var body  = await response.Body.ReadAsStringAsync();
                    var error = $"SendGrid returned {response.StatusCode}: {body}";

                    _logger.LogDebug(
                        "SendGrid: bulk chunk of {Count} failed status={Status}",
                        chunk.Length, response.StatusCode);

                    foreach (var payload in chunk)
                    {
                        allResults.Add(new NotifyResult
                        {
                            Success   = false,
                            Channel   = ChannelName,
                            Provider  = "sendgrid",
                            Recipient = payload.To,
                            Error     = error,
                            SentAt    = DateTime.UtcNow
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex,
                    "SendGrid: bulk chunk of {Count} threw", chunk.Length);

                foreach (var payload in chunk)
                {
                    allResults.Add(new NotifyResult
                    {
                        Success   = false,
                        Channel   = ChannelName,
                        Provider  = "sendgrid",
                        Recipient = payload.To,
                        Error     = ex.Message,
                        SentAt    = DateTime.UtcNow
                    });
                }
            }
        }

        return new BulkNotifyResult
        {
            Results         = allResults,
            Channel         = ChannelName,
            UsedNativeBatch = true
        };
    }
}
