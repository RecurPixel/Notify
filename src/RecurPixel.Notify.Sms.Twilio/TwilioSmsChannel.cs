using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace RecurPixel.Notify.Sms.Twilio;

/// <summary>
/// SMS channel adapter for Twilio.
/// No native bulk API — bulk is handled automatically by the base class loop.
/// </summary>
public sealed class TwilioSmsChannel : NotificationChannelBase
{
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioSmsChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "sms";

    /// <summary>
    /// Initialises a new instance of <see cref="TwilioSmsChannel"/>.
    /// </summary>
    public TwilioSmsChannel(
        IOptions<TwilioOptions> options,
        ILogger<TwilioSmsChannel> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    /// <inheritdoc />
    public override async Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Twilio SMS: attempting send to {Recipient}", payload.To);

        try
        {
            TwilioClient.Init(_options.AccountSid, _options.AuthToken);

            var message = await MessageResource.CreateAsync(
                to:   new global::Twilio.Types.PhoneNumber(payload.To),
                from: new global::Twilio.Types.PhoneNumber(_options.FromNumber),
                body: payload.Body);

            _logger.LogDebug(
                "Twilio SMS: send succeeded to {Recipient} sid={Sid}",
                payload.To, message.Sid);

            return new NotifyResult
            {
                Success    = true,
                Channel    = ChannelName,
                Provider   = "twilio",
                ProviderId = message.Sid,
                Recipient  = payload.To,
                SentAt     = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex,
                "Twilio SMS: send failed for {Recipient}", payload.To);

            return new NotifyResult
            {
                Success   = false,
                Channel   = ChannelName,
                Provider  = "twilio",
                Recipient = payload.To,
                Error     = ex.Message,
                SentAt    = DateTime.UtcNow
            };
        }
    }

    // No SendBulkAsync override — Twilio has no native SMS bulk API.
    // The base class loop handles bulk automatically.
}
