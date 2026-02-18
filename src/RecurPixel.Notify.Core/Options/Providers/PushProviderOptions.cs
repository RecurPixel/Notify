namespace RecurPixel.Notify.Core.Options.Providers;

/// <summary>
/// Firebase Cloud Messaging credentials.
/// ServiceAccountJson can be the full JSON string or an absolute file path.
/// </summary>
public class FcmOptions
{
    public string ProjectId { get; set; } = string.Empty;
    public string ServiceAccountJson { get; set; } = string.Empty;
}

/// <summary>Apple Push Notification Service credentials.</summary>
public class ApnsOptions
{
    public string KeyId { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string BundleId { get; set; } = string.Empty;
    /// <summary>The .p8 private key file content.</summary>
    public string PrivateKey { get; set; } = string.Empty;
}

/// <summary>OneSignal credentials.</summary>
public class OneSignalOptions
{
    public string AppId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>Expo Push credentials.</summary>
public class ExpoOptions
{
    public string? AccessToken { get; set; }
}