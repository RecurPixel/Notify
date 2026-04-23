---
layout: default
title: Examples
nav_order: 7
---

# Examples

Three complete setups — one per usage tier — plus a real-world production pattern.

---

## Namespace Organization (v0.2.0)

As of v0.2.0, the library uses a cleaner public API structure:

```csharp
// Core types and service
using RecurPixel.Notify;                    // INotifyService, TriggerResult, BulkTriggerResult, NotifyContext, NotifyUser
                                             // NotificationPayload, NotifyResult, NotifyOptions, etc.

// Channel interfaces
using RecurPixel.Notify.Channels;           // INotificationChannel, NotificationChannelBase, all channel implementations

// Configuration
using RecurPixel.Notify.Configuration;      // EmailOptions, SmsOptions, PushOptions, WhatsAppOptions, all provider credentials
```

> **Migrating from v0.1.0-beta.1?** If you see compiler errors like "RecurPixel.Notify.Core.Models is not found", update your using statements to match the new namespaces above.

---

## Tier 1 — Single-channel OTP service

**Use case:** Send an OTP verification email with no orchestration overhead.

**Packages:**
```bash
dotnet add package RecurPixel.Notify.Email.SendGrid
```

**Configuration (`appsettings.json`):**
```json
{
  "Notify": {
    "Email": {
      "SendGrid": {
        "ApiKey": "SG.xxxxxxxxxxxxxxxxxx",
        "FromEmail": "no-reply@yourapp.com",
        "FromName": "Your App"
      }
    }
  }
}
```

**Registration (`Program.cs`):**
```csharp
builder.Services.AddSendGridChannel(opts =>
{
    opts.ApiKey    = builder.Configuration["Notify:Email:SendGrid:ApiKey"]!;
    opts.FromEmail = builder.Configuration["Notify:Email:SendGrid:FromEmail"]!;
    opts.FromName  = builder.Configuration["Notify:Email:SendGrid:FromName"]!;
});
```

**Service:**
```csharp
using RecurPixel.Notify;        // INotificationChannel, NotificationPayload, NotifyResult
using RecurPixel.Notify.Channels;

public class OtpService(
    [FromKeyedServices("email")] INotificationChannel email,
    ILogger<OtpService> logger)
{
    public async Task<bool> SendOtpAsync(string toEmail, string otp)
    {
        var result = await email.SendAsync(new NotificationPayload
        {
            To      = toEmail,
            Subject = "Your verification code",
            Body    = $"Your one-time code is <strong>{otp}</strong>. Valid for 5 minutes."
        });

        if (!result.Success)
            logger.LogWarning("OTP email failed for {Email}: {Error}", toEmail, result.Error);

        return result.Success;
    }
}
```

---

## Tier 2 — E-commerce notifications (selective providers)

**Use case:** Order events dispatched to Email + SMS + In-App with conditions and delivery logging.

**Packages:**
```bash
dotnet add package RecurPixel.Notify
dotnet add package RecurPixel.Notify.Email.SendGrid
dotnet add package RecurPixel.Notify.Sms.Twilio
dotnet add package RecurPixel.Notify.InApp
```

**Configuration (`appsettings.json`):**
```json
{
  "Notify": {
    "Email": {
      "Provider": "sendgrid",
      "SendGrid": {
        "ApiKey": "SG.xxxxxxxxxxxxxxxxxx",
        "FromEmail": "orders@yourapp.com",
        "FromName": "Your App Orders"
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

**Registration (`Program.cs`):**
```csharp
using RecurPixel.Notify;
using RecurPixel.Notify.Configuration;

// ① InApp handler — must be registered BEFORE AddRecurPixelNotify
// This is the implementation of "sending" an in-app notification (writing to database)
builder.Services.AddInAppChannel(opts =>
    opts.UseHandler<IApplicationDbContext>(async (notification, db) =>
    {
        // UseHandler IS the send operation — you return success/failure here
        await db.Notifications.AddAsync(new Notification
        {
            UserId  = notification.UserId,
            Title   = notification.Subject ?? string.Empty,
            Message = notification.Body,
            IsRead  = false,
            SentAt  = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return new NotifyResult { Success = true };
    }));

// ② Main registration
builder.Services.AddRecurPixelNotify(
    notifyOptions =>
    {
        builder.Configuration.GetSection("Notify").Bind(notifyOptions);
    },
    orchestratorOptions =>
    {
        // ③ OnDelivery hook — fires AFTER every send (including the InApp send above)
        // This is for audit logging, metrics, or external notifications — NOT the send itself
        orchestratorOptions.OnDelivery<IApplicationDbContext>(async (result, db) =>
        {
            await db.NotificationLogs.AddAsync(new NotificationLog
            {
                Channel    = result.Channel,
                Provider   = result.Provider,
                Recipient  = result.Recipient ?? "unknown",
                Status     = result.Success ? "Sent" : "Failed",
                ProviderId = result.ProviderId,
                Error      = result.Error,
                SentAt     = result.SentAt
            });
            await db.SaveChangesAsync();
        });

        // ④ Event definitions
        orchestratorOptions
            .DefineEvent("order.placed", e => e
                .UseChannels("email", "sms", "inapp")
                .WithCondition("sms", ctx => ctx.User.PhoneVerified)
                .WithRetry(maxAttempts: 3, delayMs: 500))
            .DefineEvent("order.cancelled", e => e
                .UseChannels("email", "inapp"))
            .DefineEvent("auth.welcome", e => e
                .UseChannels("email", "inapp"));
    });
```

**Key distinction:**
- **`UseHandler`** (step ①): The code that EXECUTES the send. For InApp, where you write to your database.
- **`OnDelivery`** (step ③): An AUDIT hook that fires AFTER the send. Use it for logging, not for the send itself.

**Service:**
```csharp
using RecurPixel.Notify;

public class OrderService(INotifyService notify, ILogger<OrderService> logger)
{
    public async Task NotifyOrderPlacedAsync(Order order)
    {
        // TriggerResult contains per-channel outcomes
        var result = await notify.TriggerAsync("order.placed", new NotifyContext
        {
            User = new NotifyUser
            {
                UserId        = order.UserId.ToString(),
                Email         = order.CustomerEmail,
                Phone         = order.CustomerPhone,
                PhoneVerified = order.Customer.PhoneVerified
            },
            Channels = new()
            {
                ["email"] = new()
                {
                    Subject = $"Order #{order.Id} Confirmed",
                    Body    = $"<p>Hi {order.CustomerName}, your order has been placed.</p>"
                },
                ["sms"] = new()
                {
                    Body = $"Order #{order.Id} confirmed. Track at yourapp.com/orders/{order.Id}"
                },
                ["inapp"] = new()
                {
                    Subject  = "Order confirmed",
                    Body     = $"Order #{order.Id} is being processed.",
                    Metadata = new() { ["orderId"] = order.Id }
                }
            }
        });

        // Check the aggregated result
        if (!result.AllSucceeded)
        {
            // Some or all channels failed — inspect failures
            foreach (var failure in result.Failures)
                logger.LogWarning("Channel {Ch} failed: {Err}", failure.Channel, failure.Error);
        }
    }
}
```

---

## Tier 3 — Full SDK (all channels available)

**Use case:** New project. Install everything, configure what you need.

**Packages:**
```bash
dotnet add package RecurPixel.Notify.Sdk
dotnet add package RecurPixel.Notify.InApp
```

**Registration (`Program.cs`):**

Identical to Tier 2. The only difference is that all 30+ provider adapters are available in your project — you activate them by adding their credentials to `appsettings.json`.

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
    notifyOptions =>
    {
        builder.Configuration.GetSection("Notify").Bind(notifyOptions);
    },
    orchestratorOptions =>
    {
        orchestratorOptions
            .DefineEvent("order.placed", e => e
                .UseChannels("email", "sms", "push", "inapp")
                .WithCondition("sms",  ctx => ctx.User.PhoneVerified)
                .WithCondition("push", ctx => ctx.User.PushEnabled))
            .DefineEvent("promo.blast", e => e
                .UseChannels("email", "push"))
            .OnDelivery<IApplicationDbContext>(async (result, db) =>
            {
                await db.NotificationLogs.AddAsync(new NotificationLog
                {
                    Channel    = result.Channel,
                    Provider   = result.Provider,
                    Recipient  = result.Recipient ?? "unknown",
                    Status     = result.Success ? "Sent" : "Failed",
                    ProviderId = result.ProviderId,
                    Error      = result.Error,
                    SentAt     = result.SentAt
                });
                await db.SaveChangesAsync();
            });
    });
```

> Only providers with credentials in `appsettings.json` are registered. Unconfigured adapters are not registered at all — zero DI entries, zero runtime overhead.

---

## Production pattern

A real-world setup with InApp persistence, delivery logging, typed metadata, and multiple events. This is the correct form for apps that previously used the removed `NotifyOptions.InApp` and `NotifyOptions.OnDelivery` properties.

**Key points:**
- `AddInAppChannel(opts => opts.UseHandler<TService>(...))` replaces the removed `notifyOptions.InApp.OnDeliver(...)` pattern
- `orchestratorOptions.OnDelivery<TService>(...)` replaces the removed `notifyOptions.OnDelivery = async result => { ... }` assignment (and eliminates the `services.BuildServiceProvider()` anti-pattern — the library creates the scope for you)
- `DefineEvent` is fluent and chainable on the return value of the previous call

```csharp
// Program.cs

// ① InApp — register before AddRecurPixelNotify
services.AddInAppChannel(opts =>
    opts.UseHandler<IApplicationDbContext>(async (notification, db) =>
    {
        if (!int.TryParse(notification.UserId, out var userId))
            return new NotifyResult
            {
                Success = false,
                Error   = $"Invalid userId in InApp payload: '{notification.UserId}'"
            };

        var typeStr = notification.Metadata.TryGetValue("type", out var t) ? t?.ToString() : null;
        if (!Enum.TryParse<NotificationType>(typeStr, out var notifType))
            notifType = NotificationType.System;

        int? referenceId = null;
        if (notification.Metadata.TryGetValue("referenceId", out var rid) && rid is not null)
            referenceId = Convert.ToInt32(rid);

        var referenceType = notification.Metadata.TryGetValue("referenceType", out var rt)
            ? rt?.ToString()
            : null;

        await db.Notifications.AddAsync(new Notification
        {
            UserId        = userId,
            Title         = notification.Subject ?? string.Empty,
            Message       = notification.Body,
            Type          = notifType,
            ReferenceId   = referenceId,
            ReferenceType = referenceType,
            IsRead        = false
        });
        await db.SaveChangesAsync(CancellationToken.None);

        return new NotifyResult { Success = true };
    }));

// ② Main registration
services.AddRecurPixelNotify(
    notifyOptions =>
    {
        configuration.GetSection("Notify").Bind(notifyOptions);
    },
    orchestratorOptions =>
    {
        // ③ Delivery audit log
        // OnDelivery<TService> creates a fresh DI scope per call — no BuildServiceProvider() needed
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
            await db.SaveChangesAsync(CancellationToken.None);
        });

        // ④ Event definitions — DefineEvent returns OrchestratorOptions so calls are chainable
        orchestratorOptions
            .DefineEvent("auth.welcome",
                e => e.UseChannels("email", "inapp", "telegram").WithFallback("telegram"))
            .DefineEvent("auth.verify-email",
                e => e.UseChannels("email"))
            .DefineEvent("auth.password-reset",
                e => e.UseChannels("email"))
            .DefineEvent("order.placed",
                e => e.UseChannels("email", "inapp"))
            .DefineEvent("order.cancelled",
                e => e.UseChannels("email", "inapp"))
            .DefineEvent("order.refund",
                e => e.UseChannels("email", "inapp"))
            .DefineEvent("subscription.created",
                e => e.UseChannels("email", "inapp"))
            .DefineEvent("delivery.skipped",
                e => e.UseChannels("inapp"))
            .DefineEvent("invoice.paid",
                e => e.UseChannels("email", "inapp"))
            .DefineEvent("vendor.approved",
                e => e.UseChannels("email", "inapp"))
            .DefineEvent("vendor.suspended",
                e => e.UseChannels("email", "inapp"))
            .DefineEvent("vendor.payout",
                e => e.UseChannels("email", "inapp"));
    });
```

**Triggering an event:**
```csharp
var result = await notify.TriggerAsync("order.placed", new NotifyContext
{
    User = new NotifyUser
    {
        UserId = order.UserId.ToString(),
        Email  = order.CustomerEmail
    },
    Channels = new()
    {
        ["email"] = new()
        {
            Subject = $"Order #{order.Id} Confirmed",
            Body    = emailHtml
        },
        ["inapp"] = new()
        {
            Subject  = "Order confirmed",
            Body     = $"Order #{order.Id} is being processed.",
            Metadata = new()
            {
                ["type"]          = "Order",
                ["referenceId"]   = order.Id,
                ["referenceType"] = "Order"
            }
        }
    }
});

if (!result.AllSucceeded)
    foreach (var f in result.Failures)
        logger.LogWarning("Notification failed: {Channel} — {Error}", f.Channel, f.Error);
```
