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

# Option B: RecurPixel.Notify + only the providers you use
dotnet add package RecurPixel.Notify
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

In `Program.cs`, call `AddRecurPixelNotify` with two lambdas: the first binds your configuration, the second defines events and hooks.

```csharp
builder.Services.AddRecurPixelNotify(
    notifyOptions =>
    {
        builder.Configuration.GetSection("Notify").Bind(notifyOptions);
    },
    orchestratorOptions =>
    {
        // Define events and delivery hooks here — see Steps 5 and 6
    });
```

The library reads your configuration, validates it at startup, and registers only the providers whose credentials are present. If a required credential is missing, startup throws — never silently at send time.

---

## Step 4 — Inject and send

```csharp
public class OrderService(INotifyService notify)
{
    public async Task ConfirmOrderAsync(Order order)
    {
        var result = await notify.TriggerAsync("order.confirmed", new NotifyContext
        {
            User = new NotifyUser
            {
                UserId        = order.UserId.ToString(),
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

        if (!result.AllSucceeded)
            foreach (var f in result.Failures)
                logger.LogWarning("Channel {Ch} failed: {Err}", f.Channel, f.Error);
    }
}
```

---

## Step 5 — (Optional) Define events at startup

Events let you configure which channels fire, conditions, retry, and fallback chains — all in one place. Add them inside the second lambda in `Program.cs`:

```csharp
builder.Services.AddRecurPixelNotify(
    notifyOptions =>
    {
        builder.Configuration.GetSection("Notify").Bind(notifyOptions);
    },
    orchestratorOptions =>
    {
        orchestratorOptions.DefineEvent("order.confirmed", e => e
            .UseChannels("email", "sms")
            .WithCondition("sms", ctx => ctx.User.PhoneVerified)
            .WithRetry(maxAttempts: 3, delayMs: 500)
        );

        orchestratorOptions.DefineEvent("auth.otp", e => e
            .UseChannels("sms")
            .WithRetry(maxAttempts: 3, delayMs: 300)
        );
    });
```

If you call `TriggerAsync` with an event name that has no definition, all channels in `NotifyContext.Channels` are sent with global retry/fallback settings applied.

---

## Step 6 — (Optional) Hook into delivery results

Plug in your own delivery log writer inside the same second lambda:

```csharp
orchestratorOptions.OnDelivery<IApplicationDbContext>(async (result, db) =>
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

`OnDelivery` is called for every individual send result — including each result within a bulk operation. The typed overload `OnDelivery<TService>` resolves `TService` from a fresh DI scope per call, so scoped services like `DbContext` are safe to use directly. You own the log table. We just call the hook.

---

## Step 7 — (Optional) In-App channel

The InApp channel works differently from provider-backed channels: it has no config section and requires you to wire a handler that IS the delivery (e.g. writing to your database or pushing via SignalR). Register it as a separate call **before** `AddRecurPixelNotify`:

```csharp
builder.Services.AddInAppChannel(opts =>
    opts.UseHandler<IApplicationDbContext>(async (notification, db) =>
    {
        await db.Notifications.AddAsync(new Notification
        {
            UserId  = notification.UserId,
            Title   = notification.Subject ?? string.Empty,
            Message = notification.Body,
            IsRead  = false
        });
        await db.SaveChangesAsync();
        return new NotifyResult { Success = true };
    }));

builder.Services.AddRecurPixelNotify(
    notifyOptions => builder.Configuration.GetSection("Notify").Bind(notifyOptions),
    orchestratorOptions =>
    {
        orchestratorOptions.DefineEvent("order.confirmed", e => e
            .UseChannels("email", "sms", "inapp")
            .WithCondition("sms", ctx => ctx.User.PhoneVerified));
    });
```

> `UseHandler` provides the delivery *implementation* — it IS the send operation.
> `OnDelivery` is an audit *callback* called after every channel send. They are distinct.

---

## Next steps

- [Quick Start](quick-start) — minimal code examples for each channel
- [Usage Tiers](usage-tiers) — understand the three ways to use the library
- [Examples](examples) — complete Tier 1, 2, and 3 setups with real code
- [Adapter Reference](adapters) — all providers, their config fields, and native bulk support
- [Features](features) — retry, fallback chains, conditions, bulk send, delivery hooks
