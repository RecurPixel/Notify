using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Core.Options.Channels;

/// <summary>
/// SMS channel configuration.
/// Set Provider to the key of the provider you want to use.
/// </summary>
public class SmsOptions
{
    /// <summary>
    /// Active provider key. e.g. "twilio", "vonage", "plivo", "sinch", "messagebird", "awssns".
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Optional within-channel fallback provider key.</summary>
    public string? Fallback { get; set; }

    /// <summary>Named provider routing table.</summary>
    public Dictionary<string, NamedProviderDefinition>? Providers { get; set; }

    public TwilioOptions? Twilio { get; set; }
    public VonageOptions? Vonage { get; set; }
    public PlivoOptions? Plivo { get; set; }
    public SinchOptions? Sinch { get; set; }
    public MessageBirdOptions? MessageBird { get; set; }
    public AwsSnsOptions? AwsSns { get; set; }
    public AzureCommSmsOptions? AzureCommSms { get; set; }
}