---
layout: default
title: Quick Start
nav_order: 3
---

# Quick Start

Minimal, copy-paste-ready examples for each channel. All examples assume you have completed the [Getting Started](getting-started) setup.

---

## Using Statements

All code examples use these namespaces:

```csharp
using RecurPixel.Notify;                    // INotifyService, TriggerResult, NotifyContext, NotifyUser
using RecurPixel.Notify.Channels;           // INotificationChannel
using RecurPixel.Notify.Configuration;      // Channel options
```

---

## Email

```json
// appsettings.json
"Email": {
  "Provider": "sendgrid",
  "SendGrid": { "ApiKey": "SG.xxx", "FromEmail": "no-reply@yourapp.com", "FromName": "Your App" }
}
```

```csharp
// Orchestrated send
await notify.TriggerAsync("any.event", new NotifyContext
{
    User = new NotifyUser { Email = "user@example.com" },
    Channels = new() { ["email"] = new() { Subject = "Hello", Body = "<p>Hello world</p>" } }
});

// Direct send — no orchestrator needed
await notify.Email.SendAsync(new NotificationPayload
{
    To      = "user@example.com",
    Subject = "Hello",
    Body    = "<p>Hello world</p>"
});
```

---

## SMS

```json
"Sms": {
  "Provider": "twilio",
  "Twilio": { "AccountSid": "ACxxx", "AuthToken": "xxx", "FromNumber": "+15550001234" }
}
```

```csharp
await notify.Sms.SendAsync(new NotificationPayload
{
    To   = "+919876543210",
    Body = "Your OTP is 482910. Valid for 5 minutes."
});
```

---

## Push (FCM)

```json
"Push": {
  "Provider": "fcm",
  "Fcm": { "ProjectId": "my-project", "ServiceAccountJson": "{ ... full JSON ... }" }
}
```

```csharp
await notify.Push.SendAsync(new NotificationPayload
{
    To      = "device-registration-token",
    Subject = "New message",
    Body    = "You have a new message from Alex."
});
```

---

## WhatsApp

```json
"WhatsApp": {
  "Provider": "twilio",
  "Twilio": { "AccountSid": "ACxxx", "AuthToken": "xxx", "FromNumber": "whatsapp:+14155238886" }
}
```

```csharp
await notify.WhatsApp.SendAsync(new NotificationPayload
{
    To   = "whatsapp:+919876543210",
    Body = "Your order has been dispatched."
});
```

---

## Slack

```json
"Slack": { "WebhookUrl": "https://hooks.slack.com/services/T00/B00/xxx" }
```

```csharp
await notify.Slack.SendAsync(new NotificationPayload
{
    Subject = "Alert",
    Body    = "Payment gateway error rate exceeded 5% in the last 10 minutes."
});
```

---

## Discord

```json
"Discord": { "WebhookUrl": "https://discord.com/api/webhooks/xxx/yyy" }
```

```csharp
await notify.Discord.SendAsync(new NotificationPayload
{
    Body = "New deployment to production — v2.4.1"
});
```

---

## Teams

```json
"Teams": { "WebhookUrl": "https://outlook.office.com/webhook/xxx" }
```

```csharp
await notify.Teams.SendAsync(new NotificationPayload
{
    Subject = "Build Failed",
    Body    = "Pipeline `main` failed at step: Run Tests."
});
```

---

## Telegram

```json
"Telegram": { "BotToken": "123456:ABCdef...", "ChatId": "-1001234567890" }
```

```csharp
await notify.Telegram.SendAsync(new NotificationPayload
{
    Body = "Server CPU usage above 90% for 5 minutes."
});

// Override ChatId per message via Metadata
await notify.Telegram.SendAsync(new NotificationPayload
{
    Body     = "DM to a specific user",
    Metadata = new() { ["chat_id"] = "987654321" }
});
```

---

## Facebook Messenger

```json
"Facebook": {
  "PageAccessToken": "EAAxxxxxxxxxxxxx"
}
```

```csharp
await notify.Facebook.SendAsync(new NotificationPayload
{
    To   = "1234567890",  // recipient's page-scoped user ID (PSID)
    Body = "Your order has shipped! Track it here: yourapp.com/orders/123"
});
```

---

## LINE

```json
"Line": {
  "ChannelAccessToken": "xxxxxxxxxx",
  "ChannelSecret": "xxxxxxxxxx"
}
```

```csharp
await notify.Line.SendAsync(new NotificationPayload
{
    To   = "U1234567890abcdef",  // LINE user ID
    Body = "Your appointment is confirmed for tomorrow at 10 AM."
});
```

---

## Viber

```json
"Viber": {
  "BotAuthToken": "xxxxxxxxxx",
  "SenderName": "Your App"
}
```

```csharp
await notify.Viber.SendAsync(new NotificationPayload
{
    To   = "viber-user-id",
    Body = "Your verification code is 482910."
});
```

---

## Mattermost

```json
"Mattermost": {
  "WebhookUrl": "https://mattermost.yourapp.com/hooks/xxxxxxxxxx"
}
```

```csharp
await notify.Mattermost.SendAsync(new NotificationPayload
{
    Subject = "Deploy Complete",
    Body    = "v2.4.1 deployed to production — all health checks passing."
});
```

---

## Rocket.Chat

```json
"RocketChat": {
  "WebhookUrl": "https://rocketchat.yourapp.com/hooks/xxxxxxxxxx"
}
```

```csharp
await notify.RocketChat.SendAsync(new NotificationPayload
{
    Subject = "CI Pipeline",
    Body    = "Build #847 passed. Ready for QA review."
});
```

---

## In-App

InApp has no config section — it is wired entirely in code. Register the handler before `AddRecurPixelNotify`:

```csharp
// Program.cs — register your storage handler
builder.Services.AddInAppChannel(opts =>
    opts.UseHandler<IApplicationDbContext>(async (notification, db) =>
    {
        await db.InAppNotifications.AddAsync(new InAppNotification
        {
            UserId = notification.UserId,
            Title  = notification.Subject,
            Body   = notification.Body,
            IsRead = false
        });
        await db.SaveChangesAsync();
        return new NotifyResult { Success = true };
    }));
```

Send via orchestrator:

```csharp
await notify.TriggerAsync("any.event", new NotifyContext
{
    User     = new NotifyUser { UserId = user.Id.ToString() },
    Channels = new() { ["inapp"] = new() { Subject = "New comment", Body = "Alex replied to your post." } }
});
```

Or direct:

```csharp
await notify.InApp.SendAsync(new NotificationPayload
{
    To      = user.Id.ToString(),
    Subject = "New comment on your post",
    Body    = "Alex replied to your thread."
});
```

> `UseHandler` IS the delivery — your code saves or pushes the notification. It is not the same as `OnDelivery`, which is a post-send audit callback.

---

## Bulk send

Send to many recipients at once. Providers with native batch APIs use them automatically.

```csharp
var payloads = users.Select(u => new NotificationPayload
{
    To      = u.DeviceToken,
    Subject = "Flash sale — 2 hours only",
    Body    = "Tap to shop now.",
    Metadata = new() { ["recipient_id"] = u.Id }
}).ToList();

var result = await notify.Push.SendBulkAsync(payloads);

if (!result.AllSucceeded)
{
    foreach (var failure in result.Failures)
        logger.LogWarning("Push failed for {Recipient}: {Error}", failure.Recipient, failure.Error);
}
```

---

## Multi-channel event trigger

Fire email + SMS + push in parallel, with conditions and fallback. `orchestratorOptions` is the second parameter of `AddRecurPixelNotify`:

```csharp
// Define once at startup
orchestratorOptions.DefineEvent("order.placed", e => e
    .UseChannels("email", "sms", "push")
    .WithCondition("sms",  ctx => ctx.User.PhoneVerified)
    .WithCondition("push", ctx => ctx.User.PushEnabled)
    .WithFallback("sms", "email")
    .WithRetry(maxAttempts: 3, delayMs: 500)
);

// Trigger from your service — all active channels fire in parallel
var result = await notify.TriggerAsync("order.placed", new NotifyContext
{
    User = new NotifyUser
    {
        Email         = order.CustomerEmail,
        Phone         = order.CustomerPhone,
        DeviceToken   = order.PushToken,
        PhoneVerified = order.Customer.PhoneVerified,
        PushEnabled   = order.Customer.PushEnabled,
    },
    Channels = new()
    {
        ["email"] = new() { Subject = "Order Confirmed #" + order.Id, Body = emailHtml },
        ["sms"]   = new() { Body = $"Order #{order.Id} confirmed. Track at yourapp.com/orders" },
        ["push"]  = new() { Subject = "Order Confirmed", Body = "Tap to view your order." }
    }
});
```
