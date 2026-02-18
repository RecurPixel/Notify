using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Core.Options.Channels;

/// <summary>
/// WhatsApp channel configuration.
/// Set Provider to the key of the provider you want to use.
/// </summary>
public class WhatsAppOptions
{
    /// <summary>
    /// Active provider key. e.g. "twilio", "metacloud", "vonage".
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Optional within-channel fallback provider key.</summary>
    public string? Fallback { get; set; }

    /// <summary>Named provider routing table.</summary>
    public Dictionary<string, NamedProviderDefinition>? Providers { get; set; }

    public TwilioOptions? Twilio { get; set; }
    public MetaCloudOptions? MetaCloud { get; set; }
    public VonageWhatsAppOptions? Vonage { get; set; }
}