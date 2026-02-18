using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Channels;

namespace RecurPixel.Notify.Core.Extensions;

/// <summary>
/// Validates NotifyOptions at startup.
/// Throws InvalidOperationException immediately if configuration is incomplete or inconsistent.
/// Fail fast at startup — never fail silently at send time.
/// </summary>
internal static class NotifyOptionsValidator
{
    internal static void Validate(NotifyOptions options)
    {
        ValidateEmail(options);
        ValidateSms(options);
        ValidatePush(options);
        ValidateWhatsApp(options);
    }

    private static void ValidateEmail(NotifyOptions options)
    {
        if (options.Email is null) return;

        RequireProvider("Email", options.Email.Provider);

        switch (options.Email.Provider)
        {
            case "sendgrid":
                RequireField("Email:SendGrid:ApiKey", options.Email.SendGrid?.ApiKey);
                RequireField("Email:SendGrid:FromEmail", options.Email.SendGrid?.FromEmail);
                break;
            case "smtp":
                RequireField("Email:Smtp:Host", options.Email.Smtp?.Host);
                RequireField("Email:Smtp:FromEmail", options.Email.Smtp?.FromEmail);
                break;
            case "mailgun":
                RequireField("Email:Mailgun:ApiKey", options.Email.Mailgun?.ApiKey);
                RequireField("Email:Mailgun:Domain", options.Email.Mailgun?.Domain);
                break;
            case "resend":
                RequireField("Email:Resend:ApiKey", options.Email.Resend?.ApiKey);
                break;
            case "postmark":
                RequireField("Email:Postmark:ApiKey", options.Email.Postmark?.ApiKey);
                break;
            case "awsses":
                RequireField("Email:AwsSes:AccessKey", options.Email.AwsSes?.AccessKey);
                RequireField("Email:AwsSes:SecretKey", options.Email.AwsSes?.SecretKey);
                RequireField("Email:AwsSes:Region", options.Email.AwsSes?.Region);
                break;
            default:
                throw new InvalidOperationException(
                    $"Notify:Email:Provider '{options.Email.Provider}' is not a recognised provider.");
        }

        ValidateFallbackAndProviders("Email", options.Email.Fallback, options.Email.Providers,
            key => IsEmailProviderConfigured(options.Email, key));
    }

    private static void ValidateSms(NotifyOptions options)
    {
        if (options.Sms is null) return;

        RequireProvider("Sms", options.Sms.Provider);

        switch (options.Sms.Provider)
        {
            case "twilio":
                RequireField("Sms:Twilio:AccountSid", options.Sms.Twilio?.AccountSid);
                RequireField("Sms:Twilio:AuthToken", options.Sms.Twilio?.AuthToken);
                RequireField("Sms:Twilio:FromNumber", options.Sms.Twilio?.FromNumber);
                break;
            case "vonage":
                RequireField("Sms:Vonage:ApiKey", options.Sms.Vonage?.ApiKey);
                RequireField("Sms:Vonage:ApiSecret", options.Sms.Vonage?.ApiSecret);
                break;
            case "plivo":
                RequireField("Sms:Plivo:AuthId", options.Sms.Plivo?.AuthId);
                RequireField("Sms:Plivo:AuthToken", options.Sms.Plivo?.AuthToken);
                break;
            case "sinch":
                RequireField("Sms:Sinch:ServicePlanId", options.Sms.Sinch?.ServicePlanId);
                RequireField("Sms:Sinch:ApiToken", options.Sms.Sinch?.ApiToken);
                break;
            case "messagebird":
                RequireField("Sms:MessageBird:ApiKey", options.Sms.MessageBird?.ApiKey);
                break;
            case "awssns":
                RequireField("Sms:AwsSns:AccessKey", options.Sms.AwsSns?.AccessKey);
                RequireField("Sms:AwsSns:SecretKey", options.Sms.AwsSns?.SecretKey);
                RequireField("Sms:AwsSns:Region", options.Sms.AwsSns?.Region);
                break;
            default:
                throw new InvalidOperationException(
                    $"Notify:Sms:Provider '{options.Sms.Provider}' is not a recognised provider.");
        }
    }

    private static void ValidatePush(NotifyOptions options)
    {
        if (options.Push is null) return;

        RequireProvider("Push", options.Push.Provider);

        switch (options.Push.Provider)
        {
            case "fcm":
                RequireField("Push:Fcm:ProjectId", options.Push.Fcm?.ProjectId);
                RequireField("Push:Fcm:ServiceAccountJson", options.Push.Fcm?.ServiceAccountJson);
                break;
            case "apns":
                RequireField("Push:Apns:KeyId", options.Push.Apns?.KeyId);
                RequireField("Push:Apns:TeamId", options.Push.Apns?.TeamId);
                RequireField("Push:Apns:BundleId", options.Push.Apns?.BundleId);
                RequireField("Push:Apns:PrivateKey", options.Push.Apns?.PrivateKey);
                break;
            case "onesignal":
                RequireField("Push:OneSignal:AppId", options.Push.OneSignal?.AppId);
                RequireField("Push:OneSignal:ApiKey", options.Push.OneSignal?.ApiKey);
                break;
            case "expo":
                // AccessToken is optional for Expo
                break;
            default:
                throw new InvalidOperationException(
                    $"Notify:Push:Provider '{options.Push.Provider}' is not a recognised provider.");
        }
    }

    private static void ValidateWhatsApp(NotifyOptions options)
    {
        if (options.WhatsApp is null) return;

        RequireProvider("WhatsApp", options.WhatsApp.Provider);

        switch (options.WhatsApp.Provider)
        {
            case "twilio":
                RequireField("WhatsApp:Twilio:AccountSid", options.WhatsApp.Twilio?.AccountSid);
                RequireField("WhatsApp:Twilio:AuthToken", options.WhatsApp.Twilio?.AuthToken);
                RequireField("WhatsApp:Twilio:FromNumber", options.WhatsApp.Twilio?.FromNumber);
                break;
            case "metacloud":
                RequireField("WhatsApp:MetaCloud:AccessToken", options.WhatsApp.MetaCloud?.AccessToken);
                RequireField("WhatsApp:MetaCloud:PhoneNumberId", options.WhatsApp.MetaCloud?.PhoneNumberId);
                break;
            case "vonage":
                RequireField("WhatsApp:Vonage:ApiKey", options.WhatsApp.Vonage?.ApiKey);
                RequireField("WhatsApp:Vonage:ApiSecret", options.WhatsApp.Vonage?.ApiSecret);
                break;
            default:
                throw new InvalidOperationException(
                    $"Notify:WhatsApp:Provider '{options.WhatsApp.Provider}' is not a recognised provider.");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void RequireProvider(string channel, string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new InvalidOperationException(
                $"Notify:{channel}:Provider is required when the {channel} channel is configured.");
    }

    private static void RequireField(string path, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Notify:{path} is required.");
    }

    private static void ValidateFallbackAndProviders(
        string channel,
        string? fallback,
        Dictionary<string, Options.NamedProviderDefinition>? providers,
        Func<string, bool> isConfigured)
    {
        if (!string.IsNullOrWhiteSpace(fallback) && !isConfigured(fallback))
            throw new InvalidOperationException(
                $"Notify:{channel}:Fallback is set to '{fallback}' but its options are not configured.");

        if (providers is null) return;

        foreach (var (name, def) in providers)
        {
            if (!isConfigured(def.Type))
                throw new InvalidOperationException(
                    $"Notify:{channel}:Providers['{name}'].Type is '{def.Type}' but its options are not configured.");

            if (!string.IsNullOrWhiteSpace(def.Fallback) && !isConfigured(def.Fallback))
                throw new InvalidOperationException(
                    $"Notify:{channel}:Providers['{name}'].Fallback is '{def.Fallback}' but its options are not configured.");
        }
    }

    private static bool IsEmailProviderConfigured(Options.Channels.EmailOptions email, string key) =>
        key switch
        {
            "sendgrid" => email.SendGrid is not null,
            "smtp" => email.Smtp is not null,
            "mailgun" => email.Mailgun is not null,
            "resend" => email.Resend is not null,
            "postmark" => email.Postmark is not null,
            "awsses" => email.AwsSes is not null,
            _ => false
        };
}