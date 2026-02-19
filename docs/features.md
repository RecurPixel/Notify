---
layout: default
title: Features
nav_order: 5
---

# Features

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

Per-event config overrides the global setting for that event:

```csharp
options.Orchestrator.DefineEvent("auth.otp", e => e
    .UseChannels("sms")
    .WithRetry(maxAttempts: 5, delayMs: 300)
);
```

### Backoff schedule

With `ExponentialBackoff: true` and `DelayMs: 500`, the delays between attempts are:

| Attempt | Delay |
|---|---|
| 1 → 2 | 500 ms |
| 2 → 3 | 1,000 ms |
| 3 → 4 | 2,000 ms |

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
options.Orchestrator.DefineEvent("order.placed", e => e
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
options.Orchestrator.DefineEvent("order.placed", e => e
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
```

### BulkNotifyResult

| Property | Description |
|---|---|
| `AllSucceeded` | `true` if every send succeeded |
| `AnySucceeded` | `true` if at least one send succeeded |
| `Failures` | List of `NotifyResult` for failed sends |
| `SuccessCount` | Count of successful sends |
| `FailureCount` | Count of failed sends |
| `Total` | Total payloads submitted |
| `UsedNativeBatch` | `true` if the provider's own batch API was used |

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

Called after every individual send attempt — including each result within a bulk operation.

```csharp
options.OnDelivery(async result =>
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

### NotifyResult fields

| Field | Description |
|---|---|
| `Success` | Whether the send succeeded |
| `Channel` | Channel name (e.g. `"email"`, `"sms"`) |
| `Provider` | Provider key that actually sent (e.g. `"sendgrid"`, `"smtp"`) |
| `NamedProvider` | Named provider used for routing, if any (e.g. `"transactional"`) |
| `UsedFallback` | `true` if a fallback provider was used |
| `ProviderId` | Provider's own message ID for tracking |
| `Recipient` | Recipient identifier (`NotificationPayload.To`) |
| `Error` | Error message if `Success` is `false` |
| `SentAt` | UTC timestamp of the send attempt |

You own the log table. We call the hook. The hook is the only persistence boundary.

---

## Custom Channel Adapters

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
