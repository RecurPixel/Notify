namespace RecurPixel.Notify.Core.Options;

/// <summary>
/// Configuration for cross-channel fallback chains.
/// If a channel fails after exhausting retries, the next channel in the chain is tried.
/// This is cross-channel fallback — for within-channel provider fallback see NamedProviderDefinition.
/// </summary>
public class FallbackOptions
{
    /// <summary>
    /// Ordered list of channel names to try on failure.
    /// e.g. ["whatsapp", "sms", "email"] — tries WhatsApp first, then SMS, then email.
    /// </summary>
    public string[] Chain { get; set; } = Array.Empty<string>();
}