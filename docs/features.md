---
layout: default
title: Features
nav_order: 5
---

# Features

---

## Inspecting Results

Both `TriggerAsync` and direct channel sends return result objects that let you inspect success/failure per channel.

### TriggerResult — from `TriggerAsync`

When you call `notify.TriggerAsync("event.name", context)`, the returned `TriggerResult` contains per-channel outcomes:

```csharp
// Namespace: RecurPixel.Notify
var result = await notify.TriggerAsync("order.placed", context);

// Read the aggregate result
if (result.AllSucceeded)
    logger.LogInformation("Order notification sent to all channels");
else if (result.AnySucceeded)
    logger.LogWarning("Partial send: some channels succeeded, some failed");
else
    logger.LogError("All channels failed");

// Inspect individual channel results
foreach (var channelResult in result.ChannelResults)
{
    logger.LogInformation(
        "Channel {Ch}/{Prov} → {Status}",
        channelResult.Channel,
        channelResult.Provider ?? "default",
        channelResult.Success ? "✓ sent" : $"✗ {channelResult.Error}");
}

// Get only the failures
foreach (var failure in result.Failures)
{
    logger.LogError(
        "Channel {Ch} failed with error: {Error}",
        failure.Channel,
        failure.Error);
}
```

**`TriggerResult` properties:**

| Property         | Type                          | Description                                                     |
| ---------------- | ----------------------------- | --------------------------------------------------------------- |
| `EventName`      | `string`                      | The event name passed to `TriggerAsync`                         |
| `UserId`         | `string?`                     | User ID from the context (for tracing)                          |
| `ChannelResults` | `IReadOnlyList<NotifyResult>` | One result per channel that was dispatched                      |
| `AllSucceeded`   | `bool`                        | True if every channel in `ChannelResults` has `Success == true` |
| `AnySucceeded`   | `bool`                        | True if at least one channel succeeded                          |
| `Failures`       | `IReadOnlyList<NotifyResult>` | All results where `Success == false`                            |

### NotifyResult — individual channel outcome

Each entry in `ChannelResults` is a `NotifyResult`:

```csharp
foreach (var result in triggerResult.ChannelResults)
{
    Console.WriteLine($"Channel: {result.Channel}");                  // "email", "sms", etc.
    Console.WriteLine($"Provider: {result.Provider}");                // "sendgrid", "twilio", etc.
    Console.WriteLine($"Success: {result.Success}");                  // true or false
    Console.WriteLine($"ProviderId: {result.ProviderId}");            // Message ID from provider
    Console.WriteLine($"Recipient: {result.Recipient}");              // Email, phone, user ID, etc.
    Console.WriteLine($"Error: {result.Error}");                      // Error message (null if Success=true)
    Console.WriteLine($"UsedFallback: {result.UsedFallback}");        // true if this was a fallback channel
    Console.WriteLine($"SentAt: {result.SentAt}");                    // DateTime.UtcNow when sent
}
```

| Property        | Type       | Description                                                   |
| --------------- | ---------- | ------------------------------------------------------------- |
| `Success`       | `bool`     | Did this channel send successfully?                           |
| `Channel`       | `string`   | Logical channel name: "email", "sms", "push", "inapp", etc.   |
| `Provider`      | `string?`  | Provider name if multi-provider: "sendgrid", "twilio", etc.   |
| `NamedProvider` | `string?`  | Named provider key if you used `Metadata["provider"]` routing |
| `ProviderId`    | `string?`  | Provider's message ID (e.g., SendGrid message ID)             |
| `Recipient`     | `string?`  | Who received it: email address, phone, user ID, etc.          |
| `Error`         | `string?`  | Error message from the provider (null if `Success == true`)   |
| `UsedFallback`  | `bool`     | True if this channel fired because a previous fallback failed |
| `SentAt`        | `DateTime` | When the send occurred                                        |

### BulkTriggerResult — from `BulkTriggerAsync`

Bulk send returns one `TriggerResult` per input context:

```csharp
// Namespace: RecurPixel.Notify
var contexts = new List<NotifyContext> { /* ... */ };
var bulkResult = await notify.BulkTriggerAsync("auth.welcome", contexts);

// Aggregate check — did ALL users succeed across ALL channels?
if (bulkResult.AllSucceeded)
    logger.LogInformation("All {Count} users notified successfully", bulkResult.Results.Count);

// Were ANY successful?
if (bulkResult.AnySucceeded)
    logger.LogWarning("Partial success: {Success}/{Total} users", 
        bulkResult.Results.Count(r => r.AllSucceeded), 
        bulkResult.Results.Count);

// Inspect failures
foreach (var failure in bulkResult.Failures)
{
    logger.LogError(
        "User {UserId} failed: {Channels}",
        failure.UserId,
        string.Join(", ", failure.Failures.Select(f => $"{f.Channel}:{f.Error}")));
}
```

| Property       | Type                           | Description                                               |
| -------------- | ------------------------------ | --------------------------------------------------------- |
| `Results`      | `IReadOnlyList<TriggerResult>` | One `TriggerResult` per input context (same order)        |
| `AllSucceeded` | `bool`                         | True if EVERY user's EVERY channel succeeded              |
| `AnySucceeded` | `bool`                         | True if at least one user had at least one success        |
| `Failures`     | `IReadOnlyList<TriggerResult>` | All `TriggerResult` entries where `AllSucceeded == false` |

---

## Retry

Automatically retry failed sends with configurable delay and exponential backoff.

### Global retry

```json
"Notify": {
  "Retry": {
    "MaxAttempts": 3,
    "DelayMs": 500,
    "ExponentialBackoff": true
  }
}
```

### Per-event retry

Per-event config overrides the global setting for that event. `orchestratorOptions` is the second parameter of `AddRecurPixelNotify`:

```csharp
// Inside the orchestratorOptions => { ... } lambda
orchestratorOptions.DefineEvent("auth.otp", e => e
    .UseChannels("sms")
    .WithRetry(maxAttempts: 5, delayMs: 300)
);
```

### Backoff schedule

With `ExponentialBackoff: true` and `DelayMs: 500`, the delays between attempts are:

| Attempt | Delay    |
| ------- | -------- |
| 1 → 2   | 500 ms   |
| 2 → 3   | 1,000 ms |
| 3 → 4   | 2,000 ms |

Each attempt doubles. Retries stop when `MaxAttempts` is reached or a send succeeds.

---

## Fallback Chains

If a channel fails after exhausting retries, the next channel in the fallback chain is tried automatically. Fallback operates at the channel level — it is not the same as within-channel provider fallback.

### Global fallback

```json
"Notify": {
  "Fallback": {
    "Chain": ["whatsapp", "sms", "email"]
  }
}
```

### Per-event fallback

```csharp
orchestratorOptions.DefineEvent("order.placed", e => e
    .UseChannels("email", "sms")
    .WithFallback("whatsapp", "sms", "email")
);
```

### How it works

```
Primary: email → fails after 3 retries
Fallback: sms  → succeeds → done

Primary: email → fails
Fallback: sms  → fails
Fallback: whatsapp → succeeds → done
```

`NotifyResult.UsedFallback` is `true` whenever a fallback provider was used. Your delivery hook can track which channels are failing in production.

---

## Within-Channel Provider Fallback

Different from cross-channel fallback — this is a secondary provider for the same channel. If your primary email provider (SendGrid) goes down, the fallback (SMTP) takes over automatically.

```json
"Email": {
  "Provider": "sendgrid",
  "Fallback": "smtp",
  "SendGrid": { "ApiKey": "SG.xxx", "FromEmail": "no-reply@yourapp.com" },
  "Smtp": { "Host": "smtp.office365.com", "Port": 587, "Username": "...", "Password": "...", "UseSsl": true, "FromEmail": "no-reply@yourapp.com" }
}
```

No code change required. The library handles the switch transparently. `NotifyResult.Provider` tells you which provider actually sent the message.

---

## Named Provider Routing

Route different send types to different provider instances. Common use case: transactional email via Postmark (highest deliverability), marketing email via AWS SES (lowest cost).

```json
"Email": {
  "Provider": "sendgrid",
  "Providers": {
    "transactional": { "Type": "postmark" },
    "marketing":     { "Type": "awsses" }
  },
  "SendGrid": { ... },
  "Postmark": { ... },
  "AwsSes":   { ... }
}
```

Tag the payload:

```csharp
// Routes to Postmark
await notify.Email.SendAsync(new NotificationPayload
{
    To       = user.Email,
    Subject  = "Your OTP",
    Body     = $"OTP: {otp}",
    Metadata = new() { ["provider"] = "transactional" }
});

// Routes to AwsSes
await notify.Email.SendAsync(new NotificationPayload
{
    To       = user.Email,
    Subject  = "This week's deals",
    Body     = promoHtml,
    Metadata = new() { ["provider"] = "marketing" }
});
```

If `Metadata["provider"]` is set to a name that does not exist in the `Providers` config, an `InvalidOperationException` is thrown immediately. Named provider mismatches are loud failures, never silent reroutes.

---

## Conditions

Per-channel send conditions evaluated at runtime against the `NotifyContext`. Prevents sending to unverified numbers, users with push disabled, or any other runtime guard.

```csharp
orchestratorOptions.DefineEvent("order.placed", e => e
    .UseChannels("email", "sms", "push")
    .WithCondition("sms",  ctx => ctx.User.PhoneVerified)
    .WithCondition("push", ctx => ctx.User.PushEnabled && ctx.User.DeviceToken != null)
);
```

If a condition returns `false`, that channel is skipped entirely for this trigger. No retry, no fallback, no `OnDelivery` call — it simply doesn't fire.

---

## Parallel Dispatch

When an event targets multiple channels, all channels are dispatched in parallel via `Task.WhenAll`. Email, SMS, and Push fire simultaneously — not sequentially.

```csharp
// All three start at the same time
await notify.TriggerAsync("order.placed", context);
// ↳ Email fires  ┐
// ↳ SMS fires    ├─ all parallel
// ↳ Push fires   ┘
```

Total latency is determined by the slowest channel, not the sum of all channels.

---

## Bulk Send

Send to many recipients in a single call. Providers with native batch APIs use them automatically — you don't need to know or care which path runs.

### Direct bulk on a single channel

```csharp
var payloads = users.Select(u => new NotificationPayload
{
    To      = u.DeviceToken,
    Subject = "Flash sale — 2 hours only",
    Body    = "Tap to shop now.",
}).ToList();

var result = await notify.Push.SendBulkAsync(payloads);

Console.WriteLine($"{result.SuccessCount}/{result.Total} sent");
foreach (var failure in result.Failures)
    logger.LogWarning("Failed for {Recipient}: {Error}", failure.Recipient, failure.Error);
```

### Bulk orchestrated trigger (multi-channel, multi-user)

```csharp
var contexts = customers.Select(c => new NotifyContext
{
    User = new NotifyUser { Email = c.Email, PushEnabled = c.PushEnabled, DeviceToken = c.DeviceToken },
    Channels = new()
    {
        ["email"] = new() { Subject = "Promo", Body = promoHtml },
        ["push"]  = new() { Subject = "Promo", Body = "Tap to view." }
    }
}).ToList();

var result = await notify.BulkTriggerAsync("promo.blast", contexts);

Console.WriteLine($"{result.SuccessCount}/{result.Total} users notified");
```

### BulkNotifyResult (direct channel bulk)

| Property          | Description                                      |
| ----------------- | ------------------------------------------------ |
| `AllSucceeded`    | `true` if every send succeeded                   |
| `AnySucceeded`    | `true` if at least one send succeeded            |
| `Failures`        | List of `NotifyResult` for failed sends          |
| `SuccessCount`    | Count of successful sends                        |
| `FailureCount`    | Count of failed sends                            |
| `Total`           | Total payloads submitted                         |
| `Channel`         | Channel name shared by all results in this batch |
| `UsedNativeBatch` | `true` if the provider's own batch API was used  |

### BulkTriggerResult (orchestrated bulk)

| Property       | Description                                               |
| -------------- | --------------------------------------------------------- |
| `AllSucceeded` | `true` if every user's every channel succeeded            |
| `AnySucceeded` | `true` if at least one user/channel succeeded             |
| `Failures`     | List of `TriggerResult` where at least one channel failed |
| `SuccessCount` | Users where all channels succeeded                        |
| `FailureCount` | Users where at least one channel failed                   |
| `Total`        | Total users processed                                     |
| `Results`      | All `TriggerResult` entries in input order                |

### Bulk configuration

```json
"Notify": {
  "Bulk": {
    "ConcurrencyLimit": 10,
    "MaxBatchSize": 1000,
    "AutoChunk": true
  }
}
```

`ConcurrencyLimit` controls how many concurrent single sends run when a provider has no native bulk API. `MaxBatchSize` is the maximum payload count per native API call. `AutoChunk` automatically splits large payloads into multiple calls when the total exceeds `MaxBatchSize`.

---

## Delivery Hook

Called after every individual send attempt — including each result within a bulk operation. `orchestratorOptions` is the second parameter of `AddRecurPixelNotify`.

### Simple hook (no DI dependencies)

```csharp
orchestratorOptions.OnDelivery(async result =>
{
    Console.WriteLine($"[{result.Channel}] {(result.Success ? "OK" : "FAIL")} → {result.Recipient}");
    await Task.CompletedTask;
});
```

### Typed hook with scoped service

The typed overload resolves `TService` in a fresh DI scope per call — safe for `DbContext` and other scoped services:

```csharp
orchestratorOptions.OnDelivery<IApplicationDbContext>(async (result, db) =>
{
    await db.NotificationLogs.AddAsync(new NotificationLog
    {
        Channel       = result.Channel,
        Provider      = result.Provider,
        NamedProvider = result.NamedProvider,
        Recipient     = result.Recipient,
        Status        = result.Success ? "sent" : "failed",
        ProviderId    = result.ProviderId,
        UsedFallback  = result.UsedFallback,
        Error         = result.Error,
        SentAt        = result.SentAt
    });
    await db.SaveChangesAsync();
});
```

Multiple `OnDelivery` registrations are composable — all fire in registration order.

### NotifyResult fields

| Field           | Description                                                      |
| --------------- | ---------------------------------------------------------------- |
| `Success`       | Whether the send succeeded                                       |
| `Channel`       | Channel name (e.g. `"email"`, `"sms"`)                           |
| `Provider`      | Provider key that actually sent (e.g. `"sendgrid"`, `"smtp"`)    |
| `NamedProvider` | Named provider used for routing, if any (e.g. `"transactional"`) |
| `UsedFallback`  | `true` if a fallback provider was used                           |
| `ProviderId`    | Provider's own message ID for tracking                           |
| `Recipient`     | Recipient identifier (`NotificationPayload.To`)                  |
| `Error`         | Error message if `Success` is `false`                            |
| `SentAt`        | UTC timestamp of the send attempt                                |

You own the log table. We call the hook. The hook is the only persistence boundary.

---

## InApp Channel: `UseHandler` vs `OnDelivery`

The InApp channel is special — it requires you to wire the handler that IS the delivery, not just audit it.

### Key distinction

- **`UseHandler`** — The code that EXECUTES the send. For InApp, this is where you write to your database (or push to SignalR, or whatever your "send" means). The handler's return value determines if the send succeeded.
- **`OnDelivery`** — An audit hook that fires AFTER every send (via `UseHandler` or any other channel). It's for logging, metrics, or external notifications. The hook's success/failure does not affect the send result.

### Setup

`UseHandler` must be registered **before** calling `AddRecurPixelNotify`:

```csharp
// Program.cs

// ① InApp handler (the send implementation)
builder.Services.AddInAppChannel(opts =>
    opts.UseHandler<IApplicationDbContext>(async (notification, db) =>
    {
        await db.Notifications.AddAsync(new Notification
        {
            UserId  = notification.UserId,
            Title   = notification.Subject ?? string.Empty,
            Message = notification.Body,
            IsRead  = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        
        return new NotifyResult
        {
            Success = true,
            Channel = "inapp",
            SentAt = DateTime.UtcNow
        };
    }));

// ② Everything else (including OnDelivery hook)
builder.Services.AddRecurPixelNotify(
    notifyOptions => { /* ... */ },
    orchestratorOptions =>
    {
        // ④ OnDelivery — fires AFTER UseHandler has executed
        orchestratorOptions.OnDelivery<IApplicationDbContext>(async (result, db) =>
        {
            // This runs after every send, including the InApp send()
            // Use it for audit logging, not for the send itself
            await db.NotificationAudits.AddAsync(new NotificationAudit
            {
                Channel  = result.Channel,
                Provider = result.Provider,
                Status   = result.Success ? "sent" : "failed",
                Error    = result.Error,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });
    });
```

### Common mistake

Do NOT put your send logic in `OnDelivery`. It fires after the send, so your logic will never execute if you return a failed `NotifyResult`:

```csharp
// ❌ WRONG — This will not execute if the send fails
orchestratorOptions.OnDelivery(async result =>
{
    if (result.Success)
    {
        // This runs only if result.Success is already true
        // Too late to make the send succeed or fail
    }
});

// ✓ RIGHT — Use UseHandler for InApp delivery
builder.Services.AddInAppChannel(opts =>
    opts.UseHandler(async notification =>
    {
        // This runs BEFORE the result is determined
        // You decide what the result will be
        try
        {
            await db.Notifications.AddAsync(...);
            await db.SaveChangesAsync();
            return new NotifyResult { Success = true };
        }
        catch (Exception ex)
        {
            return new NotifyResult { Success = false, Error = ex.Message };
        }
    }));
```

---

Add your own channel by implementing `INotificationChannel`:

```csharp
public class MyPagerDutyChannel : NotificationChannelBase
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<MyPagerDutyChannel> _logger;
    private readonly string _routingKey;

    public MyPagerDutyChannel(IHttpClientFactory http, ILogger<MyPagerDutyChannel> logger, string routingKey)
    {
        _http = http;
        _logger = logger;
        _routingKey = routingKey;
    }

    public override string ChannelName => "pagerduty";

    public override async Task<NotifyResult> SendAsync(NotificationPayload payload, CancellationToken ct = default)
    {
        try
        {
            // your delivery logic
            return new NotifyResult { Success = true, Channel = ChannelName, SentAt = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            return new NotifyResult { Success = false, Channel = ChannelName, Error = ex.Message, SentAt = DateTime.UtcNow };
        }
    }
}
```

Register it:

```csharp
builder.Services.AddKeyedSingleton<INotificationChannel, MyPagerDutyChannel>("pagerduty");
```

Use it:

```csharp
await notify.TriggerAsync("critical.alert", new NotifyContext
{
    Channels = new() { ["pagerduty"] = new() { Subject = "DB down", Body = errorDetails } }
});
```

Extend `NotificationChannelBase` (not `INotificationChannel` directly) so your channel gets bulk loop support automatically.
