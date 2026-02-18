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

namespace RecurPixel.Notify.Sms.AzureCommSms;

/// <summary>
/// Notification channel adapter for Azure Communication Services SMS.
/// Supports native bulk sending via the ACS SMS batch API.
/// </summary>
public sealed class AzureCommSmsChannel : NotificationChannelBase
{
    private readonly AzureCommSmsOptions _options;
    private readonly IAzureCommSmsClient _client;
    private readonly ILogger<AzureCommSmsChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "sms";

    /// <summary>
    /// Initialises a new instance of <see cref="AzureCommSmsChannel"/>.
    /// </summary>
    public AzureCommSmsChannel(
        IOptions<AzureCommSmsOptions> options,
        IAzureCommSmsClient client,
        ILogger<AzureCommSmsChannel> logger)
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
            "AzureCommSms: sending SMS to {To}",
            payload.To);

        try
        {
            var item = await _client.SendAsync(
                from:    _options.FromNumber,
                to:      payload.To,
                message: payload.Body ?? string.Empty,
                ct:      ct);

            if (!item.Successful)
            {
                var error = $"ACS SMS error {item.StatusCode}: {item.ErrorMessage}";
                _logger.LogDebug(
                    "AzureCommSms: send failed for {To}. {Error}",
                    payload.To, error);

                return Fail(payload.To, error);
            }

            _logger.LogDebug(
                "AzureCommSms: SMS sent to {To}. MessageId {MessageId}",
                payload.To, item.MessageId);

            return new NotifyResult
            {
                Success    = true,
                Channel    = ChannelName,
                Provider   = "azurecommsms",
                ProviderId = item.MessageId,
                Recipient  = payload.To,
                SentAt     = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "AzureCommSms: exception sending to {To}",
                payload.To);

            return Fail(payload.To, ex.Message);
        }
    }

    /// <inheritdoc />
    public override async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "AzureCommSms: bulk send to {Count} recipients",
            payloads.Count);

        var allResults = new List<NotifyResult>();
        var chunks     = payloads.Chunk(100);

        foreach (var chunk in chunks)
        {
            var chunkList  = chunk.ToList();
            var recipients = chunkList.Select(p => p.To).ToList();
            var message    = chunkList[0].Body ?? string.Empty;

            try
            {
                var items = await _client.SendBulkAsync(
                    from:    _options.FromNumber,
                    to:      recipients,
                    message: message,
                    ct:      ct);

                foreach (var item in items)
                {
                    if (item.Successful)
                    {
                        allResults.Add(new NotifyResult
                        {
                            Success    = true,
                            Channel    = ChannelName,
                            Provider   = "azurecommsms",
                            ProviderId = item.MessageId,
                            Recipient  = item.To,
                            SentAt     = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        allResults.Add(Fail(item.To,
                            $"ACS SMS error {item.StatusCode}: {item.ErrorMessage}"));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "AzureCommSms: exception in bulk chunk");
                allResults.AddRange(chunkList.Select(p => Fail(p.To, ex.Message)));
            }
        }

        return new BulkNotifyResult
        {
            Results         = allResults,
            Channel         = ChannelName,
            UsedNativeBatch = true
        };
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private NotifyResult Fail(string to, string error) => new()
    {
        Success   = false,
        Channel   = ChannelName,
        Provider  = "azurecommsms",
        Recipient = to,
        Error     = error,
        SentAt    = DateTime.UtcNow
    };
}
