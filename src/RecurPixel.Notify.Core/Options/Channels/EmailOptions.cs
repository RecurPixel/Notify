using RecurPixel.Notify.Core.Options.Providers;

namespace RecurPixel.Notify.Core.Options.Channels;

/// <summary>
/// Email channel configuration.
/// Set Provider to the key of the provider you want to use.
/// Only populate the options block for the provider you have chosen.
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// Active provider key. e.g. "sendgrid", "smtp", "mailgun", "resend", "postmark", "awsses".
    /// Required if the email channel is configured.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Optional within-channel fallback provider key.
    /// Tried automatically if the primary provider fails after exhausting retries.
    /// </summary>
    public string? Fallback { get; set; }

    /// <summary>
    /// Named provider routing table.
    /// Key = the name callers use in Metadata["provider"].
    /// Value = which provider type and optional fallback to use for that name.
    /// </summary>
    public Dictionary<string, NamedProviderDefinition>? Providers { get; set; }

    public SendGridOptions? SendGrid { get; set; }
    public SmtpOptions? Smtp { get; set; }
    public MailgunOptions? Mailgun { get; set; }
    public ResendOptions? Resend { get; set; }
    public PostmarkOptions? Postmark { get; set; }
    public AwsSesOptions? AwsSes { get; set; }
    public AzureCommEmailOptions? AzureCommEmail { get; set; }
}