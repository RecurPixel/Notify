using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using dotAPNS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Push.Apns;

/// <summary>
/// Push notification channel adapter for Apple Push Notification service (APNs).
/// No native bulk API — bulk is handled automatically by the base class loop.
/// </summary>
public sealed class ApnsChannel : NotificationChannelBase
{
    private readonly ApnsOptions _options;
    private readonly IApnsClient _client;
    private readonly ILogger<ApnsChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "push";

    /// <summary>
    /// DI constructor — builds the real APNs client from options.
    /// </summary>
    public ApnsChannel(
        IOptions<ApnsOptions> options,
        ILogger<ApnsChannel> logger)
        : this(options, CreateClient(options.Value), logger) { }

    /// <summary>
    /// Internal constructor — accepts a mock client for testing.
    /// </summary>
    internal ApnsChannel(
        IOptions<ApnsOptions> options,
        IApnsClient client,
        ILogger<ApnsChannel> logger)
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
        _logger.LogDebug("APNs: attempting send to {Recipient}", payload.To);

        try
        {
            var push = new ApplePush(ApplePushType.Alert)
                .AddAlert(payload.Subject ?? string.Empty, payload.Body)
                .AddToken(payload.To);

            var response = await _client.SendAsync(push);

            if (response.IsSuccessful)
            {
                _logger.LogDebug("APNs: send succeeded to {Recipient}", payload.To);

                return new NotifyResult
                {
                    Success = true,
                    Channel = ChannelName,
                    Provider = "apns",
                    Recipient = payload.To,
                    SentAt = DateTime.UtcNow
                };
            }

            var error = response.ReasonString ?? response.Reason.ToString();

            _logger.LogDebug(
                "APNs: send failed for {Recipient} reason={Reason}",
                payload.To, error);

            return new NotifyResult
            {
                Success = false,
                Channel = ChannelName,
                Provider = "apns",
                Recipient = payload.To,
                Error = error,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "APNs: send threw for {Recipient}", payload.To);

            return new NotifyResult
            {
                Success = false,
                Channel = ChannelName,
                Provider = "apns",
                Recipient = payload.To,
                Error = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    // No SendBulkAsync override — APNs has no native bulk API.
    // The base class loop handles bulk automatically.

    private static IApnsClient CreateClient(ApnsOptions options)
    {
        var jwtOptions = new ApnsJwtOptions
        {
            BundleId = options.BundleId,
            KeyId = options.KeyId,
            TeamId = options.TeamId,
            CertContent = options.PrivateKey
        };

        return ApnsClient.CreateUsingJwt(new HttpClient(), jwtOptions);
    }
}
