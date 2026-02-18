using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Sms.AwsSns;

/// <summary>
/// Notification channel adapter for AWS Simple Notification Service SMS delivery.
/// Supports parallel bulk sending with a concurrency cap.
/// </summary>
public sealed class AwsSnsChannel : NotificationChannelBase
{
    private readonly AwsSnsOptions _options;
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly ILogger<AwsSnsChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "sms";

    /// <summary>
    /// Initialises a new instance of <see cref="AwsSnsChannel"/>.
    /// </summary>
    public AwsSnsChannel(
        IOptions<AwsSnsOptions> options,
        IAmazonSimpleNotificationService sns,
        ILogger<AwsSnsChannel> logger)
    {
        _options = options.Value;
        _sns = sns;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "AwsSns: sending SMS to {To}",
            payload.To);

        try
        {
            var request = new PublishRequest
            {
                PhoneNumber = payload.To,
                Message = payload.Body ?? string.Empty,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["AWS.SNS.SMS.SMSType"] = new MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = _options.SmsType ?? "Transactional"
                    },
                    ["AWS.SNS.SMS.SenderID"] = new MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = _options.SenderId ?? string.Empty
                    }
                }
            };

            var response = await _sns.PublishAsync(request, ct);
            var messageId = response.MessageId;

            _logger.LogDebug(
                "AwsSns: SMS sent to {To}. MessageId {MessageId}",
                payload.To, messageId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "awssns",
                ProviderId = messageId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "AwsSns: exception sending to {To}",
                payload.To);

            return Fail(payload.To, ex.Message);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// AWS SNS has no native batch SMS API for direct phone number publishing.
    /// Parallelises single sends with a concurrency cap to respect SNS rate limits.
    /// </remarks>
    public override async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "AwsSns: bulk send to {Count} recipients",
            payloads.Count);

        var semaphore = new SemaphoreSlim(BulkConcurrencyLimit);
        var tasks = payloads.Select(async payload =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var result = await SendAsync(payload, ct);
                result.Recipient = payload.To;
                return result;
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        return new BulkNotifyResult
        {
            Results = results,
            Channel = ChannelName,
            UsedNativeBatch = false
        };
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private NotifyResult Fail(string to, string error) => new()
    {
        Success = false,
        Channel = ChannelName,
        Provider = "awssns",
        Recipient = to,
        Error = error,
        SentAt = DateTime.UtcNow
    };
}
