# RecurPixel.Notify — Core philosophy, contracts, and coding rules

> Read this file for any phase involving Core, Orchestrator, or new adapters.

---

## Philosophy

### What we are
- A lightweight, idiomatic ASP.NET Core notification library
- Provider-agnostic — swap Twilio for Vonage with a config change, nothing else breaks
- Channel-agnostic — Email, SMS, Push, WhatsApp, Slack, Discord — all one interface
- Framework-friendly — built around `IServiceCollection`, `IOptions<T>`, `ILogger<T>`
- Async-first — `CancellationToken` everywhere

### What we are NOT
- Not a SaaS — no external platform, no account sign-up, no per-notification billing
- Not opinionated about message content — user owns subject, body, HTML, templates entirely
- Not opinionated about where config comes from — appsettings.json, DB, Vault, anywhere
- Not opinionated about persistence — we provide a delivery hook, user owns the log table
- Not a template engine — user brings Razor, Fluid, Handlebars, anything
- Not a queue — user calls TriggerAsync from their own worker if async dispatch needed

### Core rule
> We deliver the payload. We do not build it. We log the result hook. We do not store it.

---

## Responsibility boundary

| Concern | Owner |
|---|---|
| Message subject / body / HTML | User |
| Template engine | User |
| Channel delivery | RecurPixel.Notify |
| Provider API calls | RecurPixel.Notify |
| Where config comes from | User |
| Retry & fallback | RecurPixel.Notify |
| Notification log storage | User (via OnDelivery hook) |
| User preference management | User (passed via conditions) |
| Queue / background jobs | User |

---

## Core contracts

```csharp
// INotificationChannel — every adapter implements this via NotificationChannelBase
public interface INotificationChannel
{
    string ChannelName { get; }
    Task<NotifyResult> SendAsync(NotificationPayload payload, CancellationToken ct = default);
    Task<BulkNotifyResult> SendBulkAsync(IReadOnlyList<NotificationPayload> payloads, CancellationToken ct = default);
}

// NotificationPayload — user owns ALL content fields
public class NotificationPayload
{
    public string To        { get; set; }  // email addr, phone, device token, webhook URL
    public string Subject   { get; set; }  // email subject, push title
    public string Body      { get; set; }  // message body
    public string Recipient { get; set; }  // user ID for tracing bulk failures
    public Dictionary<string, object> Metadata { get; set; }  // channel-specific extras
}

// NotifyResult — what comes out of every send
public class NotifyResult
{
    public bool     Success       { get; set; }
    public string   Channel       { get; set; }
    public string   Provider      { get; set; }
    public string   NamedProvider { get; set; }
    public bool     UsedFallback  { get; set; }
    public string   ProviderId    { get; set; }  // provider's own message ID
    public string   Error         { get; set; }
    public DateTime SentAt        { get; set; }
    public string   Recipient     { get; set; }  // set to NotificationPayload.To for bulk tracing

    // Added in v0.3.0
    public string?  EventName     { get; set; }
    public string?  BulkBatchId   { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

// BulkNotifyResult — wraps individual results for a batch send
public class BulkNotifyResult
{
    public IReadOnlyList<NotifyResult> Results         { get; init; }
    public bool   AllSucceeded    => Results.All(r => r.Success);
    public bool   AnySucceeded    => Results.Any(r => r.Success);
    public IReadOnlyList<NotifyResult> Failures        => Results.Where(r => !r.Success).ToList();
    public int    Total           => Results.Count;
    public int    SuccessCount    => Results.Count(r => r.Success);
    public int    FailureCount    => Results.Count(r => !r.Success);
    public string Channel         { get; init; }
    public bool   UsedNativeBatch { get; init; }
}

// NotificationChannelBase — every adapter extends this, never INotificationChannel directly
public abstract class NotificationChannelBase : INotificationChannel
{
    public abstract string ChannelName { get; }
    public abstract Task<NotifyResult> SendAsync(NotificationPayload payload, CancellationToken ct = default);

    // Default bulk: loops SendAsync with concurrency cap. Override only if provider has native batch API.
    public virtual async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads, CancellationToken ct = default) { ... }

    protected virtual int BulkConcurrencyLimit => 10;
}

// INotifyService — injected via DI, orchestrated path
public interface INotifyService
{
    Task<NotifyResult>      TriggerAsync(string eventName, NotifyContext context, CancellationToken ct = default);
    Task<BulkNotifyResult>  BulkTriggerAsync(string eventName, IReadOnlyList<NotifyContext> contexts, CancellationToken ct = default);

    INotificationChannel Email    { get; }
    INotificationChannel Sms      { get; }
    INotificationChannel Push     { get; }
    INotificationChannel WhatsApp { get; }
    INotificationChannel Slack    { get; }
    INotificationChannel Discord  { get; }
    INotificationChannel Teams    { get; }
    INotificationChannel Telegram { get; }
    INotificationChannel Facebook { get; }
    INotificationChannel InApp    { get; }
}

// NotifyContext — passed to TriggerAsync
public class NotifyContext
{
    public NotifyUser User { get; set; }
    public Dictionary<string, NotificationPayload> Channels { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }  // correlation IDs, request IDs etc
}

public class NotifyUser
{
    public string UserId        { get; set; }
    public string Email         { get; set; }
    public string Phone         { get; set; }
    public string DeviceToken   { get; set; }
    public bool   PhoneVerified { get; set; }
    public bool   PushEnabled   { get; set; }
    public Dictionary<string, object> Extra { get; set; }
}
```

---

## Configuration shape

```csharp
public class NotifyOptions
{
    public EmailOptions?    Email    { get; set; }
    public SmsOptions?      Sms      { get; set; }
    public PushOptions?     Push     { get; set; }
    public WhatsAppOptions? WhatsApp { get; set; }
    public SlackOptions?    Slack    { get; set; }
    public DiscordOptions?  Discord  { get; set; }
    public TeamsOptions?    Teams    { get; set; }
    public TelegramOptions? Telegram { get; set; }
    public FacebookOptions? Facebook { get; set; }
    public InAppOptions?    InApp    { get; set; }
    public RetryOptions?    Retry    { get; set; }
    public FallbackOptions? Fallback { get; set; }
    public BulkOptions?     Bulk     { get; set; }
}

// Channel options — Provider string selects the active adapter
public class EmailOptions
{
    public string           Provider  { get; set; }  // "sendgrid"|"smtp"|"mailgun"|"resend"|"postmark"|"awsses"
    public string?          Fallback  { get; set; }  // provider key to try if primary fails
    public Dictionary<string, NamedProviderDefinition>? Providers { get; set; }  // named routing
    public SendGridOptions? SendGrid  { get; set; }
    public SmtpOptions?     Smtp      { get; set; }
    public MailgunOptions?  Mailgun   { get; set; }
    public ResendOptions?   Resend    { get; set; }
    public PostmarkOptions? Postmark  { get; set; }
    public AwsSesOptions?   AwsSes    { get; set; }
}
// SmsOptions, PushOptions, WhatsAppOptions — same pattern
```

---

## Registration — three methods, same result

```csharp
// Method 1 — IConfiguration section
builder.Services.AddRecurPixelNotify(builder.Configuration.GetSection("Notify"));

// Method 2 — fluent builder
builder.Services.AddRecurPixelNotify(options =>
{
    options.Email = new EmailOptions { Provider = "sendgrid", SendGrid = new() { ApiKey = "..." } };
});

// Method 3 — raw POCO (loaded from DB, Vault, etc.)
NotifyOptions opts = await db.GetNotifyConfigAsync();
builder.Services.AddRecurPixelNotify(opts);
```

---

## Orchestrator usage

```csharp
// Define events at startup
options.Orchestrator.DefineEvent("order.placed", e => e
    .UseChannels("email", "sms", "push")
    .WithFallback("whatsapp", "sms", "email")
    .WithCondition("sms",  ctx => ctx.User.PhoneVerified)
    .WithCondition("push", ctx => ctx.User.PushEnabled)
    .WithRetry(maxAttempts: 3, delayMs: 500)
);

// Orchestrated trigger
await notify.TriggerAsync("order.placed", new NotifyContext
{
    User = new NotifyUser { UserId = "u1", Email = "...", Phone = "..." },
    Channels = new()
    {
        ["email"] = new() { Subject = "Order Confirmed", Body = emailHtml },
        ["sms"]   = new() { Body = $"Order {id} confirmed." },
    }
});

// Direct send — bypasses orchestration, for OTP / time-critical
await notify.Sms.SendAsync(new NotificationPayload { To = phone, Body = $"OTP: {otp}" });
```

---

## Coding rules

- Language: C# 12 / .NET 8+
- Target: `netstandard2.1` for Core; `net8.0` for adapters requiring it
- DI: `Microsoft.Extensions.DependencyInjection`
- Config: `Microsoft.Extensions.Options` and `IConfiguration`
- Logging: `Microsoft.Extensions.Logging` — `ILogger<T>` only, nothing custom
- Every public method async, accepts `CancellationToken`
- Every adapter extends `NotificationChannelBase` — never `INotificationChannel` directly
- No adapter references another adapter
- No adapter contains business logic — deliver payload, return `NotifyResult`
- All `IOptions<T>` classes are plain POCOs with zero logic
- All exceptions caught inside adapters, returned as `NotifyResult { Success = false, Error = ex.Message }`
- `sealed` where inheritance is not intended
- Internal implementation classes marked `internal`
- XML doc comments on all public members
- No EF Core, Dapper, or ORM inside any package except `Dashboard.EfCore`

---

## Adapter prompt template

Use this exact prompt to generate any new adapter:

```
Create a RecurPixel.Notify channel adapter for [PROVIDER NAME].

Package name: RecurPixel.Notify.[Channel].[Provider]
Namespace:    RecurPixel.Notify.[Channel].[Provider]

Rules:
- Extend NotificationChannelBase from RecurPixel.Notify.Core (never implement INotificationChannel directly)
- ChannelName must return "[channel-name]" (lowercase, e.g. "email", "sms", "push", "whatsapp")
- SendAsync accepts NotificationPayload and CancellationToken
- Catch all exceptions, return NotifyResult { Success = false, Error = ex.Message }
- Register via AddRecurPixelNotify() extension on IServiceCollection
- Register with keyed DI: services.AddKeyedSingleton<INotificationChannel, XChannel>("{channel}:{provider}")
  e.g. "email:sendgrid", "sms:twilio", "push:fcm"
- Config class: [Provider]Options — plain POCO, no logic
- Inject config via IOptions<[Provider]Options>
- Inject ILogger<[Provider]Channel> — log attempt, success, and failure
- Do NOT add template logic, content validation, or DB access
- Do NOT reference any other channel adapter
- XML doc comments on all public members
- Unit tests using xUnit and NSubstitute

If provider has native batch API: override SendBulkAsync, set UsedNativeBatch = true
If provider has no batch API: do NOT override SendBulkAsync — base class loop handles it

Reference types in RecurPixel.Notify.Core:
- NotificationChannelBase  (extend this)
- INotificationChannel     (the interface — do not implement directly)
- NotificationPayload      (To, Subject, Body, Metadata, Recipient)
- NotifyResult             (Success, Channel, Provider, ProviderId, Error, SentAt, Recipient)
- BulkNotifyResult         (Results, Channel, UsedNativeBatch)
```

---

## Provider keyed DI registration convention

```csharp
// Pattern used throughout — channel:provider key
services.AddKeyedSingleton<INotificationChannel, SendGridChannel>("email:sendgrid");
services.AddKeyedSingleton<INotificationChannel, SmtpChannel>("email:smtp");
services.AddKeyedSingleton<INotificationChannel, TwilioSmsChannel>("sms:twilio");
services.AddKeyedSingleton<INotificationChannel, FcmChannel>("push:fcm");

// Orchestrator resolves via:
var channel = sp.GetRequiredKeyedService<INotificationChannel>($"{channelName}:{providerKey}");
```

---

*RecurPixel.Notify — Core philosophy and contracts. Updated: April 2026.*
