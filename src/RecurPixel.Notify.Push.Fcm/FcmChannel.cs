using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Push.Fcm;

/// <summary>
/// Push notification channel adapter for Firebase Cloud Messaging (FCM).
/// Single send uses the FCM messages API.
/// Bulk send uses FCM multicast — up to 500 tokens per call.
/// </summary>
public sealed class FcmChannel : NotificationChannelBase
{
    private readonly FcmOptions _options;
    private readonly IFcmMessagingClient _client;
    private readonly ILogger<FcmChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "push";

    /// <summary>
    /// Initialises a new instance of <see cref="FcmChannel"/>.
    /// </summary>
    internal FcmChannel(
        IOptions<FcmOptions> options,
        IFcmMessagingClient client,
        ILogger<FcmChannel> logger)
    {
        _options = options.Value;
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        _logger.LogDebug("FCM: attempting single send to {Recipient}", payload.To);

        try
        {
            var messageId = await _client.SendAsync(
                payload.To, payload.Subject, payload.Body, ct);

            _logger.LogDebug(
                "FCM: single send succeeded to {Recipient} messageId={MessageId}",
                payload.To, messageId);

            return new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Provider = "fcm",
                ProviderId = messageId,
                Recipient = payload.To,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "FCM: single send failed for {Recipient}", payload.To);

            return new NotifyResult
            {
                Success = false,
                Channel = ChannelName,
                Provider = "fcm",
                Recipient = payload.To,
                Error = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Sends push notifications to multiple device tokens using FCM multicast.
    /// Automatically chunks payloads into batches of up to 500 (FCM limit).
    /// Sets <see cref="BulkNotifyResult.UsedNativeBatch"/> to <c>true</c>.
    /// </summary>
    public override async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "FCM: attempting multicast for {Count} recipients", payloads.Count);

        var allResults = new List<NotifyResult>();
        var chunks = payloads.Chunk(500);

        foreach (var chunk in chunks)
        {
            try
            {
                var tokens = chunk.Select(p => p.To).ToList();
                var responses = await _client.SendMulticastAsync(
                    tokens, chunk[0].Subject, chunk[0].Body, ct);

                _logger.LogDebug(
                    "FCM: multicast chunk of {Count} complete succeeded={Succeeded} failed={Failed}",
                    chunk.Length,
                    responses.Count(r => r.IsSuccess),
                    responses.Count(r => !r.IsSuccess));

                for (var i = 0; i < chunk.Length; i++)
                {
                    var r = responses[i];
                    allResults.Add(r.IsSuccess
                        ? new NotifyResult
                        {
                            Success = true,
                            Channel = ChannelName,
                            Provider = "fcm",
                            ProviderId = r.MessageId,
                            Recipient = chunk[i].To,
                            SentAt = DateTime.UtcNow
                        }
                        : new NotifyResult
                        {
                            Success = false,
                            Channel = ChannelName,
                            Provider = "fcm",
                            Recipient = chunk[i].To,
                            Error = r.Error,
                            SentAt = DateTime.UtcNow
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex,
                    "FCM: multicast chunk of {Count} threw", chunk.Length);

                foreach (var p in chunk)
                    allResults.Add(new NotifyResult
                    {
                        Success = false,
                        Channel = ChannelName,
                        Provider = "fcm",
                        Recipient = p.To,
                        Error = ex.Message,
                        SentAt = DateTime.UtcNow
                    });
            }
        }

        return new BulkNotifyResult
        {
            Results = allResults,
            Channel = ChannelName,
            UsedNativeBatch = true
        };
    }
}
