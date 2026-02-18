namespace RecurPixel.Notify.Core.Options;

/// <summary>
/// Defines a named provider routing entry within a channel's options.
/// Users reference the name via Metadata["provider"] on the payload.
/// e.g. Metadata["provider"] = "transactional" routes to the Postmark adapter.
/// </summary>
public class NamedProviderDefinition
{
    /// <summary>
    /// The provider key this name maps to.
    /// Must match a configured provider within the same channel options.
    /// e.g. "postmark", "sendgrid", "awsses"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Optional fallback provider key if this named provider fails.
    /// Must also be configured within the same channel options.
    /// </summary>
    public string? Fallback { get; set; }
}