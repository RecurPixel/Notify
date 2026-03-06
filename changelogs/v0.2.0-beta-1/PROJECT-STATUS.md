# RecurPixel.Notify — Project Status

> **Purpose of this file:** Single authoritative handoff document. Consolidates BETA2-PLAN.md,
> BETA2-IMPLEMENTATION.md, ARCHITECTURE-DECISIONS.md, and API-CLEANUP.md into one verified-against-code
> reference. All code examples reflect the actual current codebase.

---

## 1. Project Identity

**RecurPixel.Notify** is an event-driven multi-channel notification library for ASP.NET Core.
It wraps 30+ provider adapters behind a unified `INotifyService` interface, with an orchestration
layer that handles parallel dispatch, per-channel conditions, retry with exponential backoff,
cross-channel fallback chains, and composable delivery hooks.

- **Current version:** `0.2.0-beta.1`
- **Target frameworks:** `net8.0`, `net10.0`, `netstandard2.1`
- **Primary install package:** `RecurPixel.Notify` (Core + Orchestrator merged)
- **Full SDK meta-package:** `RecurPixel.Notify.Sdk` (everything)
- **Test count:** 314 passing

---

## 2. Implementation State

### Completed (verified in code)

- `[ChannelAdapter("channel", "provider")]` attribute on every adapter class
- Assembly scanner + config filter: `EnsureAdapterAssembliesLoaded` → `DiscoverAdapters` → `IsAdapterConfigured` → `ValidateActiveProviders`
- `ConfigureAllKnownOptions` — maps every `NotifyOptions` POCO value into `IOptions<TAdapterOptions>` so DI-constructed adapters receive real credentials (not empty defaults)
- `TriggerResult` and `BulkTriggerResult` in `RecurPixel.Notify.Core.Models`
- `INotifyService.TriggerAsync` → `Task<TriggerResult>`
- `INotifyService.BulkTriggerAsync` → `Task<BulkTriggerResult>`
- Composable `OrchestratorOptions.OnDelivery` — internal `List<Func<NotifyResult, IServiceProvider, Task>>`, all handlers fire in registration order
- `OnDelivery<TService>` — scoped DI resolution per call (safe for `DbContext`)
- 14 direct channel properties on `INotifyService` (Email, Sms, Push, WhatsApp, Slack, Discord, Teams, Telegram, Facebook, InApp, Line, Viber, Mattermost, RocketChat)
- `InAppNotification` model (`UserId`, `Subject`, `Body`, `Metadata`)
- `InAppOptions.UseHandler` / `UseHandler<TService>` — wires the delivery implementation
- `AddInAppChannel` registers `"inapp:default"` with `TryAddKeyedSingleton`
- Silent no-send detection with `LogWarning` when no channel payload is found
- `ResolveAdapter` throws `InvalidOperationException` with a multi-cause diagnostic message
- `ChannelDispatcher.GetChannelConfig` covers all simple channels, throws on unknown channel names
- Core setup method is `AddNotifyOptions` (renamed from `AddRecurPixelNotify`)
- Dead properties `NotifyOptions.OnDelivery` and `NotifyOptions.InApp` removed
- `TelegramChannel` falls back to `_options.ChatId` when `payload.To` is empty
- `RecurPixel.Notify` merged meta-package exists

### Pending / Not Implemented

- **`AddRecurPixelNotify(IConfiguration, Action<OrchestratorOptions>)` overload** — only the `Action<NotifyOptions>` overload exists. Users must call `.Bind()` manually. Blocks pure appsettings-driven setup.
- **README + XML docs** — README sections (simple vs multi-provider channels, scoped services, `UseChannels` key names, `NotifyEvents` constants, beta.1 → beta.2 migration guide), XML doc updates on all `Add{X}Channel()` methods.

---

## 3. Package Structure

```
RecurPixel.Notify.Core            — interfaces, models, options, [ChannelAdapter] attribute
RecurPixel.Notify.Orchestrator    — event dispatch, NotifyService, OrchestratorOptions
RecurPixel.Notify                 — meta: Core + Orchestrator (ProjectReference only, no code)
RecurPixel.Notify.Sdk             — meta: everything

Email:     Email.SendGrid | Email.Smtp | Email.Mailgun | Email.Resend | Email.Postmark | Email.AwsSes | Email.AzureCommEmail
Sms:       Sms.Twilio | Sms.Vonage | Sms.Plivo | Sms.Sinch | Sms.MessageBird | Sms.AwsSns | Sms.AzureCommSms
Push:      Push.Fcm | Push.Apns | Push.OneSignal | Push.Expo
WhatsApp:  WhatsApp.Twilio | WhatsApp.MetaCloud | WhatsApp.Vonage
Simple:    Slack | Discord | Teams | Telegram | Facebook | Line | Viber | Mattermost | RocketChat | InApp
```

**Migration from beta.1:**
```bash
dotnet remove package RecurPixel.Notify.Core
dotnet remove package RecurPixel.Notify.Orchestrator
dotnet add package RecurPixel.Notify
```

---

## 4. Architecture

### Channel key convention
- Multi-provider channels (email, sms, push, whatsapp): `"{channel}:{provider}"` e.g. `"email:sendgrid"`
- Single-implementation channels (all others): `"{channel}:default"` e.g. `"telegram:default"`

### Startup sequence
```
AddRecurPixelNotify(configureOptions, configureOrchestrator)
  1. configureOptions(notifyOptions)          — user populates the POCO
  2. AddNotifyOptions(notifyOptions)          — validates + registers POCO singleton
  3. RegisterAdapters(services, notifyOptions)
       a. EnsureAdapterAssembliesLoaded()     — loads RecurPixel.Notify.*.dll from base dir
       b. ConfigureAllKnownOptions(...)       — maps POCO → IOptions<TAdapterOptions> for all known types
       c. DiscoverAdapters()                  — scans AppDomain for [ChannelAdapter] classes
       d. IsAdapterConfigured()               — filters: minimum credential present?
       e. TryAddKeyedSingleton(type, key)     — idempotent registration
  4. ValidateActiveProviders()               — throws at startup if Provider is set but no credential found
  5. AddRecurPixelNotifyOrchestrator(...)    — registers ChannelDispatcher (scoped), INotifyService (scoped)
```

### Key design rules
1. No config = adapter does not exist in DI — zero overhead for unused channels.
2. `IsAdapterConfigured` checks only the minimum credential (e.g. `ApiKey` for email, not `FromEmail`). Missing `FromEmail` will fail at send time.
3. `AddInAppChannel` is the only registration users must call explicitly — InApp has no JSON config section.
4. `OnDelivery` (audit hook after every send) ≠ `UseHandler` (IS the InApp send operation). They are distinct.
5. Conditions returning `false` skip the channel silently — no hook call, no error.
6. Cross-channel `WithFallback` only fires channels NOT already in `UseChannels`. Listing the same channel in both is a no-op.

### Known limitation
WhatsApp-Twilio and SMS-Twilio both use `TwilioOptions`. If both channels are configured, WhatsApp credentials overwrite SMS credentials in `IOptions<TwilioOptions>`. Named options fix deferred to a future release.

---

## 5. API Reference

### Setup

```csharp
// Program.cs

// ① InApp — always explicit, before AddRecurPixelNotify
builder.Services.AddInAppChannel(opts =>
    opts.UseHandler<IApplicationDbContext>(async (notification, db) =>
    {
        await db.InboxItems.AddAsync(new InboxItem
        {
            UserId  = int.Parse(notification.UserId),
            Title   = notification.Subject ?? string.Empty,
            Body    = notification.Body,
            IsRead  = false
        });
        await db.SaveChangesAsync();
        return new NotifyResult { Success = true };
    }));

// ② Everything else — one call
builder.Services.AddRecurPixelNotify(
    notifyOptions =>
    {
        builder.Configuration.GetSection("Notify").Bind(notifyOptions);
    },
    orchestratorOptions =>
    {
        // Events
        orchestratorOptions
            .DefineEvent("order.placed", e => e
                .UseChannels("email", "sms", "inapp")
                .WithCondition("sms", ctx => ctx.User.PhoneVerified)
                .WithRetry(maxAttempts: 3, delayMs: 500)
                .WithFallback("whatsapp", "email"))
            .DefineEvent("auth.otp", e => e
                .UseChannels("sms")
                .WithRetry(maxAttempts: 3, delayMs: 300));

        // Audit hook — scoped DbContext resolved automatically per call
        orchestratorOptions.OnDelivery<IApplicationDbContext>(async (result, db) =>
        {
            await db.NotificationLogs.AddAsync(new NotificationLog
            {
                Channel    = result.Channel,
                Provider   = result.Provider,
                Recipient  = result.Recipient ?? "Unknown",
                Status     = result.Success ? "Sent" : "Failed",
                ProviderId = result.ProviderId,
                Error      = result.Error,
                SentAt     = result.SentAt
            });
            await db.SaveChangesAsync();
        });
    });
```

### Triggering events

```csharp
// Single user
var result = await notify.TriggerAsync("order.placed", new NotifyContext
{
    User = new NotifyUser
    {
        UserId        = order.UserId.ToString(),
        Email         = order.CustomerEmail,
        Phone         = order.CustomerPhone,
        PhoneVerified = order.Customer.PhoneVerified,
        PushEnabled   = order.Customer.PushEnabled,
        DeviceToken   = order.Customer.DeviceToken
    },
    Channels = new Dictionary<string, NotificationPayload>
    {
        ["email"] = new() { Subject = "Order Confirmed", Body = emailHtml },
        ["sms"]   = new() { Body = $"Order #{order.Number} confirmed." },
        ["inapp"] = new() { Subject = "Order Confirmed", Body = "Your order is on the way." }
    }
});

if (!result.AllSucceeded)
    foreach (var f in result.Failures)
        logger.LogWarning("Channel {Ch} failed: {Err}", f.Channel, f.Error);

// Bulk (one TriggerResult per context)
var bulkResult = await notify.BulkTriggerAsync("promo.flash-sale", contexts);
```

### Direct channel access (bypasses event system)

```csharp
// Uses configured provider — no event lookup, no conditions, no fallback chain
await notify.Email.SendAsync(new NotificationPayload
{
    To      = "user@example.com",
    Subject = "Your OTP",
    Body    = $"Code: {otp}"
});

await notify.Telegram.SendAsync(new NotificationPayload
{
    // To = chat ID. If omitted, falls back to TelegramOptions.ChatId from config.
    Body = "New order received!"
});

// All 14 channels available:
// notify.Email | .Sms | .Push | .WhatsApp | .Slack | .Discord | .Teams
// notify.Telegram | .Facebook | .InApp | .Line | .Viber | .Mattermost | .RocketChat
```

### Event definition builder (full API)

```csharp
orchestratorOptions.DefineEvent("event.name", e => e
    .UseChannels("email", "sms", "push")           // which channels dispatch
    .WithCondition("sms",  ctx => ctx.User.PhoneVerified)   // skip if false
    .WithCondition("push", ctx => ctx.User.PushEnabled)
    .WithRetry(maxAttempts: 3, delayMs: 500, exponentialBackoff: true)
    .WithFallback("whatsapp", "sms", "email")      // cross-channel fallback order
);
```

> **`WithFallback` note:** Channels already in `UseChannels` are skipped in the fallback chain.
> List channels in `WithFallback` that are NOT in `UseChannels` to create a true fallback.

---

## 6. Models

### `NotifyContext`
```csharp
public class NotifyContext
{
    public NotifyUser User { get; set; }
    public Dictionary<string, NotificationPayload> Channels { get; set; }
}
```

### `NotifyUser`
```csharp
public class NotifyUser
{
    public string UserId { get; set; }          // logging/tracing only
    public string? Email { get; set; }
    public string? Phone { get; set; }          // E.164 format e.g. +14155552671
    public string? DeviceToken { get; set; }    // FCM or APNs token
    public bool PhoneVerified { get; set; }     // used in WithCondition guards
    public bool PushEnabled { get; set; }       // used in WithCondition guards
    public Dictionary<string, object> Extra { get; set; }
}
```

### `NotificationPayload`
```csharp
public class NotificationPayload
{
    public string To { get; set; }              // email address, phone, chat ID, device token etc.
    public string? Subject { get; set; }        // optional — email subject, push title
    public string Body { get; set; }            // plain text or HTML depending on channel
    public Dictionary<string, object> Metadata { get; set; }  // channel-specific extras
    // Metadata["provider"] = "myProvider"      // named provider routing
}
```

### `NotifyResult`
```csharp
public class NotifyResult
{
    public bool Success { get; set; }
    public string Channel { get; set; }
    public string? Provider { get; set; }
    public string? NamedProvider { get; set; }
    public bool UsedFallback { get; set; }
    public string? ProviderId { get; set; }     // message ID from the provider
    public string? Error { get; set; }
    public DateTime SentAt { get; set; }
    public string? Recipient { get; set; }
}
```

### `TriggerResult`
```csharp
public sealed class TriggerResult
{
    public string EventName { get; init; }
    public string? UserId { get; init; }
    public IReadOnlyList<NotifyResult> ChannelResults { get; init; }
    public bool AllSucceeded => ChannelResults.All(r => r.Success);
    public bool AnySucceeded => ChannelResults.Any(r => r.Success);
    public IReadOnlyList<NotifyResult> Failures => ...;
}
```

### `BulkTriggerResult`
```csharp
public sealed class BulkTriggerResult
{
    public IReadOnlyList<TriggerResult> Results { get; init; }  // one per context, in input order
    public bool AllSucceeded => Results.All(r => r.AllSucceeded);
    public bool AnySucceeded => Results.Any(r => r.AnySucceeded);
    public IReadOnlyList<TriggerResult> Failures => ...;
}
```

### `InAppNotification`
```csharp
public class InAppNotification
{
    public string UserId { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

---

## 7. Configuration Reference

```json
{
  "Notify": {
    "Email": {
      "Provider": "sendgrid",
      "Fallback": "smtp",
      "SendGrid": {
        "ApiKey": "SG.xxx",
        "FromEmail": "no-reply@yourapp.com",
        "FromName": "Your App"
      },
      "Smtp": {
        "Host": "smtp.example.com",
        "Port": 587,
        "Username": "user",
        "Password": "pass",
        "UseSsl": true,
        "FromEmail": "no-reply@yourapp.com",
        "FromName": "Your App"
      }
    },
    "Sms": {
      "Provider": "twilio",
      "Twilio": { "AccountSid": "ACxxx", "AuthToken": "xxx", "FromNumber": "+15550001234" }
    },
    "Push": {
      "Provider": "fcm",
      "Fcm": { "ProjectId": "my-project", "ServiceAccountJson": "{...}" }
    },
    "WhatsApp": {
      "Provider": "metacloud",
      "MetaCloud": { "AccessToken": "xxx", "PhoneNumberId": "xxx" }
    },
    "Slack":      { "WebhookUrl": "https://hooks.slack.com/..." },
    "Discord":    { "WebhookUrl": "https://discord.com/api/webhooks/..." },
    "Teams":      { "WebhookUrl": "https://outlook.office.com/webhook/..." },
    "Telegram":   { "BotToken": "1234567890:ABCdef...", "ChatId": "optional-default-chat-id" },
    "Facebook":   { "PageAccessToken": "xxx" },
    "Line":       { "ChannelAccessToken": "xxx" },
    "Viber":      { "BotAuthToken": "xxx", "SenderName": "YourApp" },
    "Mattermost": { "WebhookUrl": "https://mattermost.example.com/hooks/xxx" },
    "RocketChat": { "WebhookUrl": "https://rocketchat.example.com/hooks/xxx" },
    "Retry": {
      "MaxAttempts": 3,
      "DelayMs": 500,
      "ExponentialBackoff": true
    },
    "Bulk": {
      "ConcurrencyLimit": 10,
      "MaxBatchSize": 1000,
      "AutoChunk": true
    }
  }
}
```

**Config gotchas (common mistakes):**
- `Telegram.BotToken` — do NOT include the `bot` prefix. The URL is built as `https://api.telegram.org/bot{token}/...`.
- `Viber` — credential key is `BotAuthToken`, not `AuthToken`.
- `WhatsApp.MetaCloud` — registration check uses `AccessToken`, not `PhoneNumberId`.
- `InApp` has no config section — configure it with `AddInAppChannel(...)` in code only.
- Simple channels (Slack, Discord, etc.) have no `Provider` field — credentials go directly on the channel object.

---

## 8. Recommended Patterns

### Event name constants
```csharp
public static class NotifyEvents
{
    public const string AuthWelcome       = "auth.welcome";
    public const string AuthVerifyEmail   = "auth.verify-email";
    public const string AuthPasswordReset = "auth.password-reset";
    public const string OrderPlaced       = "order.placed";
    public const string OrderCancelled    = "order.cancelled";
    public const string InvoicePaid       = "invoice.paid";
}
```

### Named provider routing (per-send provider override)
```csharp
// appsettings.json
"Email": {
  "Provider": "sendgrid",
  "Providers": {
    "transactional": { "Type": "sendgrid" },
    "marketing":     { "Type": "mailgun", "Fallback": "sendgrid" }
  }
}

// Usage — route this send to the "marketing" provider definition
channels["email"] = new NotificationPayload
{
    To       = user.Email,
    Subject  = "Monthly digest",
    Body     = html,
    Metadata = new() { ["provider"] = "marketing" }
};
```

### Multi-`OnDelivery` composition
```csharp
orchestratorOptions
    .OnDelivery(result => { metrics.RecordSend(result.Channel, result.Success); return Task.CompletedTask; })
    .OnDelivery<INotificationLogRepository>(async (result, repo) =>
    {
        await repo.LogAsync(result);
    });
```

### Tier 1 — direct adapter injection (no orchestrator)
```csharp
builder.Services.AddSendGridChannel(new SendGridOptions
{
    ApiKey    = config["Notify:Email:SendGrid:ApiKey"],
    FromEmail = "no-reply@yourapp.com",
    FromName  = "Your App"
});

// Inject INotificationChannel keyed by "email:sendgrid" or use "email" shorthand
public class OtpService([FromKeyedServices("email:sendgrid")] INotificationChannel email) { }
```

---

## 9. Known Issues

| # | Issue | Impact | Status |
|---|-------|--------|--------|
| 1 | `AddRecurPixelNotify(IConfiguration, Action<OrchestratorOptions>)` overload missing | Users must call `.Bind()` manually | Pending |
| 2 | Twilio options collision: SMS + WhatsApp-Twilio share `IOptions<TwilioOptions>` — last write wins | Only affects users running both SMS and WhatsApp via Twilio simultaneously | Deferred |
| 3 | README + XML doc updates not started | No README migration guide, no XML docs on `Add{X}Channel()` methods | Pending |
