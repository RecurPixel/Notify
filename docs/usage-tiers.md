---
layout: default
title: Usage Tiers
nav_order: 4
---

# Usage Tiers

RecurPixel.Notify supports three usage patterns. Pick the one that fits your use case. All three share the same Core contracts — your code stays consistent no matter which tier you're on.

---

## Tier 1 — Direct Provider Usage

**Best for:** Single-channel use cases, auth flows, integrating one provider into an existing app, or situations where you want zero orchestration overhead.

Install only the provider package you need:

```bash
dotnet add package RecurPixel.Notify.Email.SendGrid
```

Register it explicitly in DI:

```csharp
builder.Services.AddSendGridChannel(options =>
{
    options.ApiKey    = builder.Configuration["SendGrid:ApiKey"];
    options.FromEmail = "no-reply@yourapp.com";
    options.FromName  = "Your App";
});
```

Inject and use the channel directly:

```csharp
public class AuthService(
    [FromKeyedServices("email")] INotificationChannel emailChannel)
{
    public async Task SendOtpAsync(string email, string otp)
    {
        var result = await emailChannel.SendAsync(new NotificationPayload
        {
            To      = email,
            Subject = "Your OTP Code",
            Body    = $"Your OTP is {otp}. Valid for 5 minutes."
        });

        if (!result.Success)
            logger.LogWarning("OTP email failed: {Error}", result.Error);
    }
}
```

**What you get:** One channel, one provider, minimal footprint. No orchestrator. No event system. No `INotifyService`.

**What you don't get:** Multi-channel dispatch, event definitions, retry/fallback chains, conditions, or `INotifyService`. Add the Orchestrator package to unlock those.

> **Note on DI registration:** Each adapter registers itself as `INotificationChannel` keyed by channel name (`"email"`, `"sms"`, `"push"`, etc.). The implementation class (`SendGridChannel`) is `internal` — you never reference it directly. You only use `INotificationChannel`.

---

## Tier 2 — Orchestrator + Selective Providers

**Best for:** Applications with multiple notification channels, event-driven sends, retry/fallback requirements, or where which provider fires should be config-driven rather than code-driven.

Install Core, Orchestrator, and the provider packages you need:

```bash
dotnet add package RecurPixel.Notify.Core
dotnet add package RecurPixel.Notify.Orchestrator
dotnet add package RecurPixel.Notify.Email.SendGrid
dotnet add package RecurPixel.Notify.Sms.Twilio
dotnet add package RecurPixel.Notify.Push.Fcm
```

Register everything through the Orchestrator's single entry point:

```csharp
builder.Services.AddRecurPixelNotify(
    builder.Configuration.GetSection("Notify")
);
```

The Orchestrator reads your `NotifyOptions`, sees which providers are configured, and calls each adapter's registration internally. You do not call `AddSendGridChannel()` yourself in Tier 2.

Optionally define events at startup:

```csharp
builder.Services.AddRecurPixelNotify(options =>
{
    options.Configure(builder.Configuration.GetSection("Notify"));

    options.Orchestrator.DefineEvent("order.placed", e => e
        .UseChannels("email", "sms", "push")
        .WithCondition("sms",  ctx => ctx.User.PhoneVerified)
        .WithCondition("push", ctx => ctx.User.PushEnabled)
        .WithFallback("whatsapp", "sms", "email")
        .WithRetry(maxAttempts: 3, delayMs: 500)
    );
});
```

Inject `INotifyService` and trigger events or send directly:

```csharp
public class OrderService(INotifyService notify)
{
    public async Task PlaceOrderAsync(Order order)
    {
        // Orchestrated — event config drives which channels fire
        await notify.TriggerAsync("order.placed", context);

        // Direct — bypass orchestration for time-critical sends
        await notify.Email.SendAsync(otpPayload);
        await notify.Sms.SendAsync(otpPayload);
    }
}
```

**What you get:** Everything — `INotifyService`, event definitions, parallel dispatch, conditions, retry with exponential backoff, cross-channel fallback chains, bulk send via `BulkTriggerAsync`, delivery hooks, and direct channel access via `notify.Email`, `notify.Sms`, etc.

---

## Tier 3 — Full SDK Meta-Package

**Best for:** New projects that want everything configured and ready with minimal setup decisions.

```bash
dotnet add package RecurPixel.Notify.Sdk
```

The SDK meta-package pulls Core + Orchestrator + all provider adapters. Registration is identical to Tier 2:

```csharp
builder.Services.AddRecurPixelNotify(
    builder.Configuration.GetSection("Notify")
);
```

Only providers you configure in `appsettings.json` are active. Unconfigured providers are registered but dormant — they add no runtime overhead.

---

## Comparison

| | Tier 1 | Tier 2 | Tier 3 |
|---|---|---|---|
| Manual provider registration | ✅ Required | ❌ Automatic | ❌ Automatic |
| `INotifyService` | ❌ Not available | ✅ Yes | ✅ Yes |
| Event definitions | ❌ No | ✅ Yes | ✅ Yes |
| Retry / fallback | ❌ No | ✅ Yes | ✅ Yes |
| Conditions | ❌ No | ✅ Yes | ✅ Yes |
| Parallel dispatch | ❌ No | ✅ Yes | ✅ Yes |
| Bulk send | ✅ Via channel directly | ✅ Yes | ✅ Yes |
| Delivery hook | ❌ No | ✅ Yes | ✅ Yes |
| Package footprint | Minimal | Medium | Larger |
| Config-driven routing | ❌ No | ✅ Yes | ✅ Yes |

---

## Can I mix tiers?

Yes. The most common pattern is using Tier 2 (Orchestrator) for most sends, while also calling individual channels directly for time-critical flows like OTP:

```csharp
// Orchestrated — event config, conditions, fallback all apply
await notify.TriggerAsync("order.placed", context);

// Direct — fires immediately, no event lookup, no conditions
await notify.Sms.SendAsync(new NotificationPayload { To = phone, Body = $"OTP: {otp}" });
```

Direct channel access on `INotifyService` bypasses the event system but still goes through the configured provider. It is the correct pattern for auth flows and any send where latency matters.
