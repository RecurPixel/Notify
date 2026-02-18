using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Core.Options.Channels;

/// <summary>
/// Push notification channel configuration.
/// Set Provider to the key of the provider you want to use.
/// </summary>
public class PushOptions
{
    /// <summary>
    /// Active provider key. e.g. "fcm", "apns", "onesignal", "expo".
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Optional within-channel fallback provider key.</summary>
    public string? Fallback { get; set; }

    /// <summary>Named provider routing table.</summary>
    public Dictionary<string, NamedProviderDefinition>? Providers { get; set; }

    public FcmOptions? Fcm { get; set; }
    public ApnsOptions? Apns { get; set; }
    public OneSignalOptions? OneSignal { get; set; }
    public ExpoOptions? Expo { get; set; }
}