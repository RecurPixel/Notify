---
layout: default
title: Getting Started
nav_order: 2
---

# Getting Started

This guide walks you from a fresh .NET project to sending your first notification.

---

## Prerequisites

- .NET 8 or later
- An ASP.NET Core application (or any .NET host with `IServiceCollection`)
- API credentials for at least one provider (e.g. a SendGrid API key, Twilio account, etc.)

---

## Step 1 — Install packages

Choose the packages you need. You do not need to install everything.

```bash
# Option A: Full SDK — pulls all channels and providers
dotnet add package RecurPixel.Notify.Sdk

# Option B: Core + Orchestrator + only the providers you use
dotnet add package RecurPixel.Notify.Core
dotnet add package RecurPixel.Notify.Orchestrator
dotnet add package RecurPixel.Notify.Email.SendGrid
dotnet add package RecurPixel.Notify.Sms.Twilio

# Option C: Single channel, no orchestrator
dotnet add package RecurPixel.Notify.Email.SendGrid
```

See [Usage Tiers](usage-tiers) to understand which option fits your use case.

---

## Step 2 — Add configuration

Add a `Notify` section to `appsettings.json`:

```json
{
  "Notify": {
    "Email": {
      "Provider": "sendgrid",
      "SendGrid": {
        "ApiKey": "SG.xxxxxxxxxxxxxxxxxx",
        "FromEmail": "no-reply@yourapp.com",
        "FromName": "Your App"
      }
    },
    "Sms": {
      "Provider": "twilio",
      "Twilio": {
        "AccountSid": "ACxxxxxxxxxxxxxxxxxx",
        "AuthToken": "xxxxxxxxxxxxxxxxxx",
        "FromNumber": "+15550001234"
      }
    },
    "Retry": {
      "MaxAttempts": 3,
      "DelayMs": 500,
      "ExponentialBackoff": true
    }
  }
}
```

You only need to configure channels you actually use. Unconfigured channels are ignored.

---

## Step 3 — Register in DI

In `Program.cs`:

```csharp
builder.Services.AddRecurPixelNotify(
    builder.Configuration.GetSection("Notify")
);
```

That's it. The library reads your configuration, validates it at startup, and registers everything in DI. If your config is missing a required field (like `ApiKey`), it throws at startup — never silently at send time.

---

## Step 4 — Inject and send

```csharp
public class OrderService(INotifyService notify)
{
    public async Task ConfirmOrderAsync(Order order)
    {
        await notify.TriggerAsync("order.confirmed", new NotifyContext
        {
            User = new NotifyUser
            {
                UserId        = order.UserId,
                Email         = order.CustomerEmail,
                Phone         = order.CustomerPhone,
                PhoneVerified = order.Customer.PhoneVerified,
            },
            Channels = new Dictionary<string, NotificationPayload>
            {
                ["email"] = new() { Subject = "Order Confirmed", Body = emailHtml },
                ["sms"]   = new() { Body = $"Your order #{order.Id} is confirmed." }
            }
        });
    }
}
```

---

## Step 5 — (Optional) Define events at startup

Events let you configure which channels fire, conditions, retry, and fallback chains — all in one place. Define them in `Program.cs` before building the app:

```csharp
builder.Services.AddRecurPixelNotify(options =>
{
    options.Configure(builder.Configuration.GetSection("Notify"));

    options.Orchestrator.DefineEvent("order.confirmed", e => e
        .UseChannels("email", "sms")
        .WithCondition("sms", ctx => ctx.User.PhoneVerified)
        .WithRetry(maxAttempts: 3, delayMs: 500)
    );

    options.Orchestrator.DefineEvent("auth.otp", e => e
        .UseChannels("sms")
        .WithRetry(maxAttempts: 3, delayMs: 300)
    );
});
```

If you call `TriggerAsync` with an event name that has no definition, all channels in `NotifyContext.Channels` are sent with global retry/fallback settings applied.

---

## Step 6 — (Optional) Hook into delivery results

Plug in your own delivery log writer:

```csharp
options.OnDelivery(async result =>
{
    await db.NotificationLogs.AddAsync(new NotificationLog
    {
        Channel    = result.Channel,
        Provider   = result.Provider,
        Recipient  = result.Recipient,
        Status     = result.Success ? "sent" : "failed",
        ProviderId = result.ProviderId,
        Error      = result.Error,
        SentAt     = result.SentAt
    });
    await db.SaveChangesAsync();
});
```

`OnDelivery` is called for every individual send result — including each result within a bulk operation. You own the log table. We just call the hook.

---

## Next steps

- [Quick Start](quick-start) — minimal code examples for each channel
- [Usage Tiers](usage-tiers) — understand the three ways to use the library
- [Adapter Reference](adapters) — all providers, their config fields, and native bulk support
- [Features](features) — retry, fallback chains, conditions, bulk send, delivery hooks
