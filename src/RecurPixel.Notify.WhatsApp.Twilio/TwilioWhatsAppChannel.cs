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

namespace RecurPixel.Notify.WhatsApp.Twilio;

/// <summary>
/// WhatsApp channel adapter using the Twilio WhatsApp API.
/// No native bulk API — bulk is handled automatically by the base class loop.
/// </summary>
public sealed class TwilioWhatsAppChannel : NotificationChannelBase
{
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioWhatsAppChannel> _logger;

    /// <inheritdoc />
    public override string ChannelName => "whatsapp";

    /// <summary>
    /// Initialises a new instance of <see cref="TwilioWhatsAppChannel"/>.
    /// </summary>
    public TwilioWhatsAppChannel(
        IOptions<TwilioOptions> options,
        ILogger<TwilioWhatsAppChannel> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }

    /// <inheritdoc />
    public override async Task<NotifyResult> SendAsync(
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Twilio WhatsApp: attempting send to {Recipient}", payload.To);

        try
        {
            TwilioClient.Init(_options.AccountSid, _options.AuthToken);

            // Twilio requires the whatsapp: prefix on both from and to numbers
            var from = _options.FromNumber.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase)
                ? _options.FromNumber
                : $"whatsapp:{_options.FromNumber}";

            var to = payload.To.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase)
                ? payload.To
                : $"whatsapp:{payload.To}";

            var message = await MessageResource.CreateAsync(
                to:   new global::Twilio.Types.PhoneNumber(to),
                from: new global::Twilio.Types.PhoneNumber(from),
                body: payload.Body);

            _logger.LogDebug(
                "Twilio WhatsApp: send succeeded to {Recipient} sid={Sid}",
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
                "Twilio WhatsApp: send failed for {Recipient}", payload.To);

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

    // No SendBulkAsync override — Meta policy restricts bulk WhatsApp.
    // The base class loop handles bulk automatically.
}
