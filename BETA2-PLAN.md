# RecurPixel.Notify — v0.1.0-beta.2 Planning Document

> All changes to implement before tagging beta.2. Work through in order — blocking bugs first, then breaking changes, then DX, then docs.

---

## Work Order

1. **Blocking Bugs** — fix first, everything else depends on stable dispatch
2. **Architecture Changes** — registration keys, OnDelivery split, InApp handler
3. **Breaking API Changes** — TriggerAsync return type, package naming
4. **DX Improvements** — composable hooks, single setup call, validation errors
5. **Documentation** — README sections, XML doc corrections

---

## 1. Blocking Bugs

---

### 1.1 Channel Registration Key Pattern — Root Fix

**Problem**

`ChannelDispatcher.GetChannelConfig` only handles `email`, `sms`, `push`, `whatsapp` explicitly. Everything else — Slack, Discord, Teams, InApp, Telegram, Facebook, Line, Viber — falls through to `(null, null, null)`. The dispatcher then tries to resolve a bare key like `"inapp"`, but the registration is `"inapp:inapp"`. Hard crash on every send for all simple channels.

**Root Cause**

The bug is in the dispatcher, not the registration pattern. The `"channel:provider"` pattern is correct and must stay consistent across everything. The fix is to extend dispatcher coverage and adopt `"channel:default"` for single-implementation channels.

**The Rule Going Forward**

- Multi-provider channels (`email`, `sms`, `push`, `whatsapp`): register as `"channel:providername"` e.g. `"email:sendgrid"`, `"sms:twilio"`
- Single-implementation channels (`slack`, `discord`, `inapp`, etc.): register as `"channel:default"` e.g. `"slack:default"`, `"inapp:default"`
- If a single-implementation channel gains a second adapter in future, it adopts the full `"channel:provider"` pattern — that is a minor version breaking config change, not an architecture change

**Dispatcher Resolution Logic — Updated**

```csharp
private string ResolveKey(string channel, NotificationPayload payload)
{
    // Step 1 — named provider routing via Metadata
    if (payload.Metadata?.TryGetValue("provider", out var named) == true)
        return $"{channel}:{named}";

    // Step 2 — configured provider for multi-provider channels
    var (provider, _, _) = GetChannelConfig(channel);
    if (!string.IsNullOrEmpty(provider))
        return $"{channel}:{provider}";

    // Step 3 — single-implementation channels fall through to :default
    return $"{channel}:default";
}
```

**GetChannelConfig — Updated**

```csharp
private (string? provider, string? fallback, Dictionary<string, NamedProviderDefinition>? namedProviders)
    GetChannelConfig(string channel)
{
    return channel switch
    {
        "email"    => (options.Email?.Provider,    options.Email?.Fallback,    options.Email?.Providers),
        "sms"      => (options.Sms?.Provider,      options.Sms?.Fallback,      options.Sms?.Providers),
        "push"     => (options.Push?.Provider,     options.Push?.Fallback,     options.Push?.Providers),
        "whatsapp" => (options.WhatsApp?.Provider, options.WhatsApp?.Fallback, options.WhatsApp?.Providers),

        // Single-implementation channels — no provider selection, no fallback, no named routing
        // Dispatcher falls through to "{channel}:default" key
        "slack"    or "discord" or "teams"    or
        "telegram" or "facebook" or "line"    or
        "viber"    or "inapp"                 => (null, null, null),

        _ => throw new InvalidOperationException(
            $"Unknown channel '{channel}'. If this is a custom channel, ensure it is registered correctly.")
    };
}
```

**Registration — All Simple Channels Updated**

```csharp
// Before (wrong)
services.AddKeyedSingleton<INotificationChannel, InAppChannel>("inapp:inapp");
services.AddKeyedSingleton<INotificationChannel, SlackChannel>("slack:slack");

// After (correct)
services.AddKeyedSingleton<INotificationChannel, InAppChannel>("inapp:default");
services.AddKeyedSingleton<INotificationChannel, SlackChannel>("slack:default");
services.AddKeyedSingleton<INotificationChannel, DiscordChannel>("discord:default");
services.AddKeyedSingleton<INotificationChannel, TeamsChannel>("teams:default");
services.AddKeyedSingleton<INotificationChannel, TelegramChannel>("telegram:default");
services.AddKeyedSingleton<INotificationChannel, FacebookChannel>("facebook:default");
services.AddKeyedSingleton<INotificationChannel, LineChannel>("line:default");
services.AddKeyedSingleton<INotificationChannel, ViberChannel>("viber:default");
```

---

### 1.2 `IOptions<NotifyOptions>` Not Registered

**Problem**

`AddRecurPixelNotify` calls `services.AddSingleton(options)` — the raw POCO. `ChannelDispatcher` injects `IOptions<NotifyOptions>` and receives an empty default instance. `Email.Provider` is null. Wrong adapter key is built. Wrong or missing adapter resolved.

**Fix**

```csharp
// Before (wrong)
services.AddSingleton(options);

// After (correct)
services.AddSingleton<IOptions<NotifyOptions>>(Options.Create(options));

// Also add the raw POCO for anything that injects NotifyOptions directly
services.AddSingleton(options);
```

Both registrations are needed — `IOptions<NotifyOptions>` for framework-pattern consumers, `NotifyOptions` directly for internal classes that don't use the options wrapper.

---

### 1.3 Misleading XML Doc on `AddSmtpChannel`

**Fix**

```csharp
// Before (wrong)
/// <summary>
/// Registers the SMTP email channel. Called internally by AddRecurPixelNotify().
/// </summary>

// After (correct)
/// <summary>
/// Registers the SMTP email channel adapter with the DI container.
/// Call this method explicitly in your Program.cs after AddRecurPixelNotify().
/// </summary>
/// <example>
/// builder.Services.AddRecurPixelNotify(config.GetSection("Notify"), orchestrator => { ... });
/// builder.Services.AddSmtpChannel();
/// </example>
```

Apply the same correction to the XML docs of every other `Add{Channel}` extension method.

---

## 2. Architecture Changes

---

### 2.1 Config Shape for Webhook-Only Channels

**Rule**

If a channel has one implementation today, no `Provider` field is required in config. The user configures only the credentials. Internally the dispatcher always resolves `"channel:default"`.

If a channel gains a second adapter in a future version, `Provider` becomes a required field and startup validation enforces it. This is a config-level breaking change at that point, not an architecture change.

**Current Config — Correct Shape**

```json
"Slack": {
  "WebhookUrl": "https://hooks.slack.com/services/xxx"
},
"Discord": {
  "WebhookUrl": "https://discord.com/api/webhooks/xxx"
},
"Teams": {
  "WebhookUrl": "https://outlook.office.com/webhook/xxx"
},
"Telegram": {
  "BotToken": "xxx",
  "ChatId": "xxx"
},
"Facebook": {
  "PageAccessToken": "xxx",
  "AppSecret": "xxx"
}
```

No `Provider` field. No nested object. Credentials directly on the channel options.

**Future Config — When a Second Adapter Exists (example: Slack)**

```json
"Slack": {
  "Provider": "webhook",
  "Webhook": {
    "WebhookUrl": "https://hooks.slack.com/services/xxx"
  },
  "BotApi": {
    "BotToken": "xoxb-xxx",
    "DefaultChannel": "#alerts"
  }
}
```

Migration path: `WebhookUrl` moves under `Webhook`. `Provider` becomes required. Startup validation throws if `Provider` is missing with a clear message: `"Notify:Slack:Provider is required. Valid values: webhook, botapi"`.

---

### 2.2 OnDelivery Split — Audit Hook vs InApp Delivery

**The Distinction**

These are two fundamentally different things that must not share a mechanism:

|         | `OnDelivery`             | `InApp.OnDeliver`                      |
| ------- | ------------------------ | -------------------------------------- |
| Purpose | Post-send audit          | IS the send                            |
| Fires   | After every channel send | When InApp channel is dispatched       |
| Returns | void                     | `NotifyResult`                         |
| Failure | Log it                   | Notification not delivered — can retry |
| Scope   | All channels             | InApp only                             |

**`OnDelivery` — Global Audit Hook**

Fires after every send attempt on every channel, including InApp. Used for audit logging, metrics, alerting. Does not affect delivery outcome.

```csharp
// Registration
orchestrator.OnDelivery(async result =>
{
    logger.LogInformation("Delivered via {Channel}/{Provider} — {Status}",
        result.Channel, result.Provider, result.Success ? "ok" : "failed");
});

// With scoped services (see section 4.2)
orchestrator.OnDelivery<AppDbContext>(async (result, db) =>
{
    await db.NotificationLogs.AddAsync(new NotificationLog
    {
        Channel    = result.Channel,
        Provider   = result.Provider,
        Recipient  = result.Recipient,
        Success    = result.Success,
        Error      = result.Error,
        SentAt     = result.SentAt
    });
    await db.SaveChangesAsync();
});
```

**`InApp.OnDeliver` — Delivery Mechanism**

This replaces the InApp "adapter." It IS the delivery. Returns `NotifyResult` so failures surface and can be retried by the orchestrator.

```csharp
// Registration — separate from AddRecurPixelNotify
builder.Services.AddInAppChannel(inApp =>
{
    inApp.OnDeliver<AppDbContext>(async (notification, db) =>
    {
        await db.InboxItems.AddAsync(new InboxItem
        {
            UserId    = notification.UserId,
            Title     = notification.Subject,
            Body      = notification.Body,
            CreatedAt = DateTime.UtcNow,
            IsRead    = false
        });
        await db.SaveChangesAsync();

        return new NotifyResult
        {
            Success  = true,
            Channel  = "inapp",
            Provider = "default",
            SentAt   = DateTime.UtcNow
        };
    });
});
```

**InAppNotification — New Model**

```csharp
/// <summary>
/// Payload passed to the InApp delivery handler.
/// Contains the full notification context — user, subject, body, and metadata.
/// </summary>
public sealed class InAppNotification
{
    public string UserId   { get; init; }
    public string Subject  { get; init; }
    public string Body     { get; init; }
    public Dictionary<string, object> Metadata { get; init; }
}
```

**Sequence — How Both Hooks Fire**

```
TriggerAsync("order.placed", context)
    → dispatches "inapp" channel
        → calls InApp.OnDeliver(notification, db)   ← delivery happens here
        → returns NotifyResult { Success = true }
    → OnDelivery(result) fires                       ← audit happens here
    → result stored in TriggerResult.ChannelResults
```

---

## 3. Breaking API Changes

---

### 3.1 `TriggerAsync` Return Type

**Problem**

`TriggerAsync` currently returns a single aggregated `NotifyResult`. When an event fires both `"email"` and `"inapp"`, result merges them: `Channel = "email,inapp"`, `Provider = "smtp"`, `Recipient` = email only. No way to inspect per-channel outcomes from the return value.

**Fix — New `TriggerResult`**

```csharp
/// <summary>
/// Result of a TriggerAsync call.
/// Contains individual NotifyResult per channel dispatched.
/// </summary>
public sealed class TriggerResult
{
    /// <summary>All individual channel results, one per channel dispatched.</summary>
    public IReadOnlyList<NotifyResult> ChannelResults { get; init; }

    /// <summary>True only if every channel succeeded.</summary>
    public bool AllSucceeded => ChannelResults.All(r => r.Success);

    /// <summary>True if at least one channel succeeded.</summary>
    public bool AnySucceeded => ChannelResults.Any(r => r.Success);

    /// <summary>All failed channel results.</summary>
    public IReadOnlyList<NotifyResult> Failures => ChannelResults.Where(r => !r.Success).ToList();

    /// <summary>The event name that was triggered.</summary>
    public string EventName { get; init; }
}
```

**Updated `INotifyService`**

```csharp
// Before
Task<NotifyResult> TriggerAsync(string eventName, NotifyContext context, CancellationToken ct = default);

// After
Task<TriggerResult> TriggerAsync(string eventName, NotifyContext context, CancellationToken ct = default);
```

**Usage**

```csharp
var result = await notify.TriggerAsync("order.placed", context);

if (!result.AllSucceeded)
{
    foreach (var failure in result.Failures)
        logger.LogWarning("Channel {Channel} failed: {Error}", failure.Channel, failure.Error);
}

// Inspect individual channels
var emailResult = result.ChannelResults.FirstOrDefault(r => r.Channel == "email");
var inAppResult = result.ChannelResults.FirstOrDefault(r => r.Channel == "inapp");
```

---

### 3.2 Package Naming Restructure

**Current State (confusing)**

```
RecurPixel.Notify.Core           → interfaces only, not usable alone
RecurPixel.Notify.Orchestrator   → required for any real usage
RecurPixel.Notify.Sdk            → all adapters bundled
RecurPixel.Notify                → does not exist on NuGet
```

**Target State**

```
RecurPixel.Notify                → NEW: Core + Orchestrator merged, the base install
RecurPixel.Notify.Email.SendGrid → adapter, depends on RecurPixel.Notify
RecurPixel.Notify.Email.Smtp     → adapter
... (all other adapters unchanged)
RecurPixel.Notify.Sdk            → meta-package, pulls everything
```

**Migration for existing beta.1 users**

```
Remove: RecurPixel.Notify.Core
Remove: RecurPixel.Notify.Orchestrator
Add:    RecurPixel.Notify
```

All adapter packages stay the same — they now depend on `RecurPixel.Notify` instead of `RecurPixel.Notify.Core`.

**Typical install after restructure**

```bash
# Minimal — email only
dotnet add package RecurPixel.Notify
dotnet add package RecurPixel.Notify.Email.Smtp

# Ecom stack
dotnet add package RecurPixel.Notify
dotnet add package RecurPixel.Notify.Email.SendGrid
dotnet add package RecurPixel.Notify.Sms.Twilio
dotnet add package RecurPixel.Notify.Push.Fcm
dotnet add package RecurPixel.Notify.InApp

# Full SDK
dotnet add package RecurPixel.Notify.Sdk
```

---

## 4. DX Improvements

---

### 4.1 Single Setup Call

**Problem**

Users must call both `AddRecurPixelNotify(options)` and `AddRecurPixelNotifyOrchestrator(configure)` separately. Forgetting either causes a runtime DI failure with no obvious link back to setup.

**Fix — Combined Overload**

```csharp
// New primary overload — config section
builder.Services.AddRecurPixelNotify(
    builder.Configuration.GetSection("Notify"),
    orchestrator =>
    {
        orchestrator.DefineEvent("order.placed", e => e
            .UseChannels("email", "sms", "inapp")
            .WithCondition("sms", ctx => ctx.User.PhoneVerified)
            .WithRetry(maxAttempts: 3, delayMs: 500)
        );

        orchestrator.OnDelivery<AppDbContext>(async (result, db) =>
        {
            await db.NotificationLogs.AddAsync(...);
            await db.SaveChangesAsync();
        });
    }
);

// New primary overload — fluent options
builder.Services.AddRecurPixelNotify(
    options =>
    {
        options.Email = new EmailOptions
        {
            Provider = "sendgrid",
            SendGrid = new() { ApiKey = Environment.GetEnvironmentVariable("SG_KEY") }
        };
    },
    orchestrator =>
    {
        orchestrator.DefineEvent("order.placed", e => e.UseChannels("email"));
    }
);

// Existing overload (keep for compatibility — orchestrator optional)
builder.Services.AddRecurPixelNotify(builder.Configuration.GetSection("Notify"));
```

The two-argument overloads replace the separate `AddRecurPixelNotifyOrchestrator` call. The single-argument overload stays for cases where orchestration is configured separately or not used.

---

### 4.2 Scoped Services Inside Hooks

**Problem**

`OnDelivery` and `InApp.OnDeliver` are singleton-lifetime. Injecting `DbContext` directly causes a captive dependency. Users have no SDK guidance for this.

**Fix — Typed Overloads with Internal Scope Management**

```csharp
// OnDelivery with scoped service
orchestrator.OnDelivery<AppDbContext>(async (result, db) =>
{
    await db.NotificationLogs.AddAsync(new NotificationLog { ... });
    await db.SaveChangesAsync();
});

// InApp.OnDeliver with scoped service
inApp.OnDeliver<AppDbContext>(async (notification, db) =>
{
    await db.InboxItems.AddAsync(new InboxItem { ... });
    await db.SaveChangesAsync();
    return new NotifyResult { Success = true, Channel = "inapp" };
});
```

**Internal Implementation**

```csharp
// OrchestratorOptions — internal implementation of typed OnDelivery
public OrchestratorOptions OnDelivery<TService>(
    Func<NotifyResult, TService, Task> handler)
    where TService : class
{
    _deliveryHandlers.Add(async (result, serviceProvider) =>
    {
        using var scope = serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        await handler(result, service);
    });
    return this;
}
```

The library creates and disposes the scope internally. User code just receives the service they need.

---

### 4.3 Composable `OnDelivery` Registrations

**Problem**

Calling `OnDelivery` twice silently drops the first registration.

**Fix — Internal list, all handlers fire**

```csharp
// Both of these register and both fire — no silent overwrite
orchestrator.OnDelivery(async result =>
{
    metrics.Increment($"notify.{result.Channel}.{(result.Success ? "sent" : "failed")}");
});

orchestrator.OnDelivery<AppDbContext>(async (result, db) =>
{
    await db.NotificationLogs.AddAsync(...);
});
```

**Internal Implementation**

```csharp
// Before — single delegate, overwrites
public Func<NotifyResult, Task>? DeliveryHook { get; private set; }
public OrchestratorOptions OnDelivery(Func<NotifyResult, Task> handler)
{
    DeliveryHook = handler; // silent overwrite
    return this;
}

// After — list of delegates, all fire
private readonly List<Func<NotifyResult, IServiceProvider, Task>> _deliveryHandlers = new();

internal async Task InvokeDeliveryHandlers(NotifyResult result, IServiceProvider sp)
{
    foreach (var handler in _deliveryHandlers)
        await handler(result, sp);
}
```

---

### 4.4 Silent No-Send Detection

**Problem**

`UseChannels("email:smtp")` (wrong — provider suffix on channel name) produces `Success = true` with nothing dispatched and no log output.

**Fix — Validate at dispatch time, warn loudly**

```csharp
// Inside TriggerAsync dispatch loop
foreach (var channelName in eventDef.Channels)
{
    if (!context.Channels.ContainsKey(channelName))
    {
        logger.LogWarning(
            "Event '{EventName}' targets channel '{Channel}' but NotifyContext.Channels " +
            "has no entry for '{Channel}'. No notification was sent for this channel. " +
            "Check that UseChannels() uses logical channel names (e.g. \"email\"), " +
            "not provider names (e.g. \"email:sendgrid\").",
            eventName, channelName, channelName);

        // Include in TriggerResult so caller can detect it
        channelResults.Add(new NotifyResult
        {
            Success  = false,
            Channel  = channelName,
            Error    = $"No payload provided for channel '{channelName}' in NotifyContext.Channels.",
            SentAt   = DateTime.UtcNow
        });

        continue;
    }

    // ... proceed with dispatch
}
```

---

### 4.5 Event Name Constants Pattern — Documentation

No code change needed. Document this pattern prominently in the README and in XML docs on `DefineEvent`.

```csharp
// Recommended — define all event names as constants to prevent typo bugs
public static class NotifyEvents
{
    public const string OrderPlaced      = "order.placed";
    public const string OrderShipped     = "order.shipped";
    public const string OrderDelivered   = "order.delivered";
    public const string AuthOtp          = "auth.otp";
    public const string PasswordReset    = "auth.password-reset";
    public const string PromoBlast       = "marketing.promo";
}

// Setup
orchestrator.DefineEvent(NotifyEvents.OrderPlaced, e => e
    .UseChannels("email", "sms", "inapp")
);

// Trigger
await notify.TriggerAsync(NotifyEvents.OrderPlaced, context);
```

---

## 5. Documentation Updates

---

### 5.1 "Simple Channel" vs "Multi-Provider Channel" — README Section

Add a dedicated section explaining:

- Multi-provider channels: `email`, `sms`, `push`, `whatsapp` — require `Provider` in config, support named routing and within-channel fallback
- Simple channels: `slack`, `discord`, `teams`, `telegram`, `facebook`, `line`, `viber`, `inapp` — no `Provider` field, no named routing, no within-channel fallback
- How simple channels evolve to multi-provider when a second adapter is added

---

### 5.2 Scoped Services Pattern — README Section

Full example of using `DbContext` inside hooks using the typed overloads. Include a note that the raw `OnDelivery(Func<NotifyResult, Task>)` overload is for cases that don't need scoped services (e.g. calling an already-singleton logger or metrics client).

---

### 5.3 `UseChannels` vs `NotifyContext.Channels` Keys — README Section

Document explicitly:

```csharp
// CORRECT — UseChannels takes logical channel names
e.UseChannels("email", "sms", "push")

// CORRECT — NotifyContext.Channels uses the same logical channel names
Channels = new()
{
    ["email"] = new() { Subject = "...", Body = "..." },
    ["sms"]   = new() { Body = "..." },
    ["push"]  = new() { Subject = "...", Body = "..." }
}

// WRONG — do not use provider names in UseChannels
e.UseChannels("email:sendgrid")  // silent no-send in beta.1, warning+error in beta.2
```

---

### 5.4 `DefineEvent` + `TriggerAsync` Relationship — README Section

Document that `DefineEvent` must be called before `TriggerAsync` for any given event name, and that a missing event definition throws at runtime. Recommend the `NotifyEvents` constants pattern to eliminate typo bugs.

---

### 5.5 Core + Orchestrator Now Merged — Migration Note

Add to README and NuGet description:

```
Migrating from beta.1?
  dotnet remove package RecurPixel.Notify.Core
  dotnet remove package RecurPixel.Notify.Orchestrator
  dotnet add package RecurPixel.Notify
All adapter packages (RecurPixel.Notify.Email.*, etc.) are unchanged.
```

---

## Summary — Change Checklist

### Blocking Bugs
- [ ] Fix `GetChannelConfig` to cover all simple channels
- [ ] Update all simple channel registrations to `"channel:default"` key
- [ ] Register `IOptions<NotifyOptions>` correctly in `AddRecurPixelNotify`
- [ ] Add{Provider}Channel() methods are retained for Tier 1 direct-injection usage. Their internal implementation changes — they no longer register the adapter (scanning handles that) and instead add a non-keyed INotificationChannel alias pointing at the already-registered keyed service. XML docs updated to reflect this.

### Architecture
- [ ] Remove `Provider` field from single-implementation channel options (Slack, Discord, etc.)
- [ ] Implement `InApp.OnDeliver` as separate registration from global `OnDelivery`
- [ ] Add `InAppNotification` model
- [ ] Update `InAppChannel` to invoke `InApp.OnDeliver` and return its `NotifyResult`

### Breaking API
- [ ] Add `TriggerResult` with `IReadOnlyList<NotifyResult> ChannelResults`
- [ ] Update `INotifyService.TriggerAsync` to return `Task<TriggerResult>`
- [ ] Merge Core + Orchestrator into single `RecurPixel.Notify` package
- [ ] Update all adapter `.csproj` dependencies from `RecurPixel.Notify.Core` to `RecurPixel.Notify`

### DX
- [ ] Add `AddRecurPixelNotify(IConfiguration, Action<OrchestratorOptions>)` overload
- [ ] Add `AddRecurPixelNotify(Action<NotifyOptions>, Action<OrchestratorOptions>)` overload
- [ ] Add `OnDelivery<TService>` typed overload with internal scope management
- [ ] Add `InApp.OnDeliver<TService>` typed overload
- [ ] Make `OnDelivery` composable — internal list, all handlers fire
- [ ] Add silent no-send detection and warning log in dispatch loop

### Documentation
- [ ] Add "Simple vs Multi-Provider Channels" section to README
- [ ] Add scoped services pattern section to README
- [ ] Add `UseChannels` vs `NotifyContext.Channels` keys section to README
- [ ] Add `NotifyEvents` constants pattern to README
- [ ] Add beta.1 → beta.2 migration note to README
- [ ] Update CHANGELOG.md — move all items above from planned to fixed

---

*RecurPixel.Notify — v0.1.0-beta.2 Plan. March 2026.*
