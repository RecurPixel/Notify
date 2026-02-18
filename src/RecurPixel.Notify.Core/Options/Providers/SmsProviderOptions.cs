namespace RecurPixel.Notify.Core.Options.Providers;

/// <summary>Twilio credentials. Used for SMS and WhatsApp.</summary>
public class TwilioOptions
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}

/// <summary>Vonage (Nexmo) credentials.</summary>
public class VonageOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}

/// <summary>Plivo credentials.</summary>
public class PlivoOptions
{
    public string AuthId { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}

/// <summary>Sinch credentials.</summary>
public class SinchOptions
{
    public string ServicePlanId { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}

/// <summary>MessageBird credentials.</summary>
public class MessageBirdOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Originator { get; set; } = string.Empty;
}

/// <summary>AWS SNS credentials.</summary>
public class AwsSnsOptions
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    /// <summary>Optional SMS type: "Transactional" (default) or "Promotional".</summary>
    public string? SmsType { get; set; }
    /// <summary>Optional sender ID (may not be supported in all regions).</summary>
    public string? SenderId { get; set; }
}