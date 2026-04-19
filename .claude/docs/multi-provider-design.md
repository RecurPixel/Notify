# RecurPixel.Notify — Multi-provider, fallback & named routing design

> Read this file for any phase involving the Orchestrator, multi-provider resolution,
> or within-channel fallback.

---

## Two features, one options shape

### Feature 1 — Within-channel fallback (redundancy)

Primary provider fails after exhausting retries → fallback provider tried automatically.

```
SendGrid → fails → retry x3 → still fails → SMTP → success → done
```

Zero code change in user code. Configured entirely in options.

### Feature 2 — Named providers (routing)

Multiple named provider instances for the same channel. User tags a payload via `Metadata["provider"]`
and the library routes to that specific provider. On failure, falls back to the named provider's
configured fallback, or surfaces the error.

```
Metadata["provider"] = "transactional" → routes to Postmark
Metadata["provider"] = "marketing"     → routes to AwsSes
(no tag)                               → routes to default Provider
```

Both features coexist. Fallback is automatic failure handling. Named routing is intentional routing.

---

## Options shape

```csharp
public class EmailOptions
{
    // Default provider — used when no named provider is specified
    public string Provider { get; set; }  // "sendgrid"|"smtp"|"mailgun"|"resend"|"postmark"|"awsses"

    // Feature 1 — within-channel fallback
    public string? Fallback { get; set; }  // provider key to try if default fails after retries

    // Feature 2 — named providers
    // Key = name user references in Metadata["provider"]
    // Value = which provider type to use + optional fallback for that named provider
    public Dictionary<string, NamedProviderDefinition>? Providers { get; set; }

    // Provider credentials — only populate the ones you use
    public SendGridOptions? SendGrid { get; set; }
    public SmtpOptions?     Smtp     { get; set; }
    public MailgunOptions?  Mailgun  { get; set; }
    public ResendOptions?   Resend   { get; set; }
    public PostmarkOptions? Postmark { get; set; }
    public AwsSesOptions?   AwsSes   { get; set; }
}

public class NamedProviderDefinition
{
    public string  Type     { get; set; }   // "sendgrid"|"smtp"|"postmark"|"awsses" etc
    public string? Fallback { get; set; }   // optional: named provider's own fallback
}
```

Same pattern applies to `SmsOptions`, `PushOptions`, `WhatsAppOptions`.

---

## appsettings.json examples

### Fallback only

```json
"Email": {
  "Provider": "sendgrid",
  "Fallback": "smtp",
  "SendGrid": { "ApiKey": "SG.xxx", "FromEmail": "no-reply@example.com" },
  "Smtp": { "Host": "smtp.office365.com", "Port": 587, "Username": "...", "Password": "...", "UseSsl": true, "FromEmail": "no-reply@example.com" }
}
```

### Named providers only

```json
"Email": {
  "Provider": "sendgrid",
  "Providers": {
    "transactional": { "Type": "postmark" },
    "marketing":     { "Type": "awsses" }
  },
  "SendGrid": { "ApiKey": "SG.xxx", "FromEmail": "no-reply@example.com" },
  "Postmark": { "ApiKey": "xxx", "FromEmail": "no-reply@example.com" },
  "AwsSes":   { "AccessKey": "xxx", "SecretKey": "xxx", "Region": "us-east-1", "FromEmail": "marketing@example.com" }
}
```

### Both together

```json
"Email": {
  "Provider": "sendgrid",
  "Fallback": "smtp",
  "Providers": {
    "transactional": { "Type": "postmark", "Fallback": "sendgrid" },
    "marketing":     { "Type": "awsses",   "Fallback": "smtp" }
  },
  "SendGrid": { "ApiKey": "SG.xxx", "FromEmail": "no-reply@example.com" },
  "Smtp":     { "Host": "smtp.office365.com", "Port": 587, "Username": "...", "Password": "...", "UseSsl": true, "FromEmail": "no-reply@example.com" },
  "Postmark": { "ApiKey": "xxx", "FromEmail": "no-reply@example.com" },
  "AwsSes":   { "AccessKey": "xxx", "SecretKey": "xxx", "Region": "us-east-1", "FromEmail": "marketing@example.com" }
}
```

---

## User code

### Fallback — zero code change

```csharp
// Nothing changes in user code — fallback is entirely automatic
await notify.TriggerAsync("order.placed", new NotifyContext
{
    User = notifyUser,
    Channels = new() { ["email"] = new() { Subject = "Order Confirmed", Body = emailHtml } }
});
```

### Named routing — add Metadata["provider"]

```csharp
// Routes to Postmark
await notify.TriggerAsync("order.placed", new NotifyContext
{
    User = notifyUser,
    Channels = new()
    {
        ["email"] = new()
        {
            Subject  = "Order Confirmed",
            Body     = emailHtml,
            Metadata = new() { ["provider"] = "transactional" }
        }
    }
});

// Direct send with named provider
await notify.Email.SendAsync(new NotificationPayload
{
    To       = user.Email,
    Subject  = "Your OTP",
    Body     = $"OTP: {otp}",
    Metadata = new() { ["provider"] = "transactional" }
});
```

If `Metadata["provider"]` is set but the name does not exist in `Providers` config:
throw `InvalidOperationException` immediately with a clear message.
Never silently fall back to default — loud failure only.

---

## Internal resolution logic (Orchestrator implementation)

```
Dispatch(channel: "email", payload)
│
├── Has Metadata["provider"]?
│   ├── YES → look up name in EmailOptions.Providers
│   │         ├── Found → resolve adapter → SendAsync
│   │         │           ├── Success → done
│   │         │           └── Fail → has NamedProviderDefinition.Fallback?
│   │         │                       ├── YES → resolve fallback adapter → SendAsync
│   │         │                       └── NO  → log failure, surface error
│   │         └── Not found → throw InvalidOperationException (loud failure)
│   │
│   └── NO  → use EmailOptions.Provider (default)
│             resolve default adapter → SendAsync
│             ├── Success → done
│             └── Fail (after retries) → has EmailOptions.Fallback?
│                         ├── YES → resolve fallback adapter → SendAsync
│                         └── NO  → log failure, surface error
```

---

## Keyed DI registration

Adapters are registered keyed by `"{channel}:{provider}"`:

```csharp
services.AddKeyedSingleton<INotificationChannel, SendGridChannel>("email:sendgrid");
services.AddKeyedSingleton<INotificationChannel, PostmarkChannel>("email:postmark");
services.AddKeyedSingleton<INotificationChannel, SmtpChannel>("email:smtp");
services.AddKeyedSingleton<INotificationChannel, TwilioSmsChannel>("sms:twilio");
services.AddKeyedSingleton<INotificationChannel, FcmChannel>("push:fcm");
```

Orchestrator resolves via:

```csharp
var adapter = serviceProvider.GetRequiredKeyedService<INotificationChannel>($"{channel}:{providerKey}");
```

Channel adapters themselves are unaware of multi-provider logic. Each adapter implements a single
`SendAsync` — it does not know about fallbacks or named routing. Resolution happens in the
Orchestrator dispatch layer before `SendAsync` is called.

---

## NotifyResult — multi-provider fields

```csharp
public class NotifyResult
{
    public bool   Success       { get; set; }
    public string Channel       { get; set; }
    public string Provider      { get; set; }   // which provider actually sent it
    public string NamedProvider { get; set; }   // the name used for routing, if any
    public bool   UsedFallback  { get; set; }   // true if fallback provider was used
    public string ProviderId    { get; set; }
    public string Error         { get; set; }
    public DateTime SentAt      { get; set; }
}
```

---

## Startup validation rules

Run inside `AddRecurPixelNotify()`. Fail fast — never fail silently at send time.

```csharp
// If Fallback is set, that provider's options must be configured
if (!string.IsNullOrEmpty(options.Email?.Fallback))
{
    if (!IsProviderConfigured(options.Email, options.Email.Fallback))
        throw new InvalidOperationException(
            $"Notify:Email:Fallback is '{options.Email.Fallback}' but its options are not configured.");
}

// If Providers dict is set, each Type and its Fallback must have options configured
if (options.Email?.Providers is not null)
{
    foreach (var (name, def) in options.Email.Providers)
    {
        if (!IsProviderConfigured(options.Email, def.Type))
            throw new InvalidOperationException(
                $"Notify:Email:Providers['{name}'].Type is '{def.Type}' but its options are not configured.");

        if (!string.IsNullOrEmpty(def.Fallback) && !IsProviderConfigured(options.Email, def.Fallback))
            throw new InvalidOperationException(
                $"Notify:Email:Providers['{name}'].Fallback is '{def.Fallback}' but its options are not configured.");
    }
}
```

---

## Rules summary

- Multi-provider resolution lives in the Orchestrator dispatch layer only
- Channel adapters are unaware of multi-provider logic — they implement one `SendAsync`
- Resolution order: `Metadata["provider"]` named routing → default `Provider` → `Fallback`
- Named provider not found → throw `InvalidOperationException` immediately, loud failure
- Never silently route to default when a named provider is missing
- `NotifyResult.Provider` = which provider actually sent (not which was tried first)
- `NotifyResult.UsedFallback = true` when fallback provider was used
- `NotifyResult.NamedProvider` = the named routing key, if named routing was used
- All startup validation runs in `AddRecurPixelNotify()` — fail fast, never at send time

---

*RecurPixel.Notify — Multi-provider design. Updated: April 2026.*
