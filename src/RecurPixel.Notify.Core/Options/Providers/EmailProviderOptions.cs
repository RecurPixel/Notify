namespace RecurPixel.Notify.Core.Options.Providers;

/// <summary>SendGrid provider credentials.</summary>
public class SendGridOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

/// <summary>SMTP provider credentials. Works with any SMTP server.</summary>
public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

/// <summary>Mailgun provider credentials.</summary>
public class MailgunOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

/// <summary>Resend provider credentials.</summary>
public class ResendOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

/// <summary>Postmark provider credentials.</summary>
public class PostmarkOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

/// <summary>AWS SES provider credentials.</summary>
public class AwsSesOptions
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}