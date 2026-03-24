---
layout: default
title: Home
nav_order: 1
permalink: /
---

# RecurPixel.Notify

A modular, DI-native NuGet notification library for ASP.NET Core. Drop it in. Bring your own API keys. Own your data.
{: .fs-6 .fw-300 }

**✅ v0.2.0 STABLE** — Production-ready with 35+ adapters across 13+ channels
{: .fs-5 }

[Get Started](getting-started){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[Quick Start](quick-start){: .btn .fs-5 .mb-4 .mb-md-0 }

> **Migrating from v0.1.0-beta.1?**  
> The namespace structure has been reorganized in v0.2.0. See the [Migration Guide](#migration-from-v010-beta1) below.

---

## What It Is

RecurPixel.Notify is a pure .NET library — not a platform, not SaaS, no external dependency. It handles multi-channel notification delivery (Email, SMS, Push, WhatsApp, Slack, Discord, Teams, Mattermost, Rocket.Chat, Telegram, Facebook, LINE, Viber, In-App) through a single consistent interface.

**You bring:** your API keys, your message content, your delivery log table.
**We handle:** provider API calls, retry with exponential backoff, cross-channel fallback chains, parallel dispatch, and delivery hooks.

---

## Install

```bash
# Full SDK — everything included
dotnet add package RecurPixel.Notify.Sdk

# Or install only what you need
dotnet add package RecurPixel.Notify
dotnet add package RecurPixel.Notify.Email.SendGrid
dotnet add package RecurPixel.Notify.Sms.Twilio
```

See [Usage Tiers](usage-tiers) to understand which option fits your use case.

---

## Adapter Status

| Package                | Provider                          | Channel   | Unit Tested | Integration Tested | Community Approved |
| ---------------------- | --------------------------------- | --------- | ----------- | ------------------ | ------------------ |
| `Email.SendGrid`       | Twilio SendGrid                   | Email     | ✅           | ✅                  | 🔲                  |
| `Email.Smtp`           | Any SMTP server                   | Email     | ✅           | ✅                  | 🔲                  |
| `Email.Mailgun`        | Mailgun                           | Email     | ✅           | 🔲                  | 🔲                  |
| `Email.Resend`         | Resend                            | Email     | ✅           | 🔲                  | 🔲                  |
| `Email.Postmark`       | Postmark                          | Email     | ✅           | 🔲                  | 🔲                  |
| `Email.AwsSes`         | AWS SES                           | Email     | ✅           | 🔲                  | 🔲                  |
| `Email.AzureCommEmail` | Azure Communication Services      | Email     | ✅           | 🔲                  | 🔲                  |
| `Sms.Twilio`           | Twilio                            | SMS       | ✅           | ✅                  | 🔲                  |
| `Sms.Vonage`           | Vonage (Nexmo)                    | SMS       | ✅           | 🔲                  | 🔲                  |
| `Sms.Plivo`            | Plivo                             | SMS       | ✅           | 🔲                  | 🔲                  |
| `Sms.Sinch`            | Sinch                             | SMS       | ✅           | 🔲                  | 🔲                  |
| `Sms.MessageBird`      | MessageBird                       | SMS       | ✅           | 🔲                  | 🔲                  |
| `Sms.AwsSns`           | AWS SNS                           | SMS       | ✅           | 🔲                  | 🔲                  |
| `Sms.AzureCommSms`     | Azure Communication Services      | SMS       | ✅           | 🔲                  | 🔲                  |
| `Push.Fcm`             | Firebase Cloud Messaging          | Push      | ✅           | 🔲                  | 🔲                  |
| `Push.Apns`            | Apple Push Notification Service   | Push      | ✅           | 🔲                  | 🔲                  |
| `Push.OneSignal`       | OneSignal                         | Push      | ✅           | 🔲                  | 🔲                  |
| `Push.Expo`            | Expo Push                         | Push      | ✅           | 🔲                  | 🔲                  |
| `WhatsApp.Twilio`      | Twilio WhatsApp                   | WhatsApp  | ✅           | ✅                  | 🔲                  |
| `WhatsApp.MetaCloud`   | Meta Cloud API                    | WhatsApp  | ✅           | 🔲                  | 🔲                  |
| `WhatsApp.Vonage`      | Vonage WhatsApp                   | WhatsApp  | ✅           | 🔲                  | 🔲                  |
| `Slack`                | Slack Webhooks / Bot API          | Team Chat | ✅           | ✅                  | 🔲                  |
| `Discord`              | Discord Webhooks                  | Team Chat | ✅           | ✅                  | 🔲                  |
| `Teams`                | Microsoft Teams Webhooks          | Team Chat | ✅           | 🔲                  | 🔲                  |
| `Mattermost`           | Mattermost Webhooks               | Team Chat | ✅           | 🔲                  | 🔲                  |
| `RocketChat`           | Rocket.Chat Webhooks              | Team Chat | ✅           | 🔲                  | 🔲                  |
| `Facebook`             | Meta Messenger API                | Social    | ✅           | 🔲                  | 🔲                  |
| `Telegram`             | Telegram Bot API                  | Social    | ✅           | ✅                  | 🔲                  |
| `Line`                 | LINE Messaging API                | Social    | ✅           | 🔲                  | 🔲                  |
| `Viber`                | Viber Business Messages           | Social    | ✅           | 🔲                  | 🔲                  |
| `InApp`                | Hook-based (user-defined storage) | In-App    | ✅           | ✅                  | 🔲                  |

**Legend:** ✅ Complete · 🔲 Not yet · ⚠️ Partial

---

## Design Principles

- **Zero infrastructure** — pure library, no platform to host or sign up for
- **Provider agnostic** — swap Twilio for Vonage with a config change, nothing else breaks
- **DI-native** — registers via `AddRecurPixelNotify()`, injected as `INotifyService`
- **Config agnostic** — accepts `IConfiguration`, options builder, or a raw POCO
- **Content agnostic** — we deliver the payload, you build the subject and body
- **Hook-based logging** — `OnDelivery()` callback, you write to your own DB

---

## What's Coming in v0.3.0

v0.3.0 introduces a **Dashboard package** for delivery tracking and observability:

- **RecurPixel.Notify.Dashboard** — New observability package with delivery logs, batch tracking, and embedded HTML dashboard
- **NotificationLog Entity & INotificationLogStore** — Pluggable persistence (SQL Server, PostgreSQL, SQLite, custom implementations)
- **BulkBatchId Grouping** — Track all notifications in a bulk send as a unit
- **REST API** — Query logs, filter by channel/status/date, retrieve provider responses
- **Circuit Breaker Pattern** — Auto-disable broken channels without code changes
- **Community Adapter Approval** — Peer-reviewed providers earn special status
- **Scheduled Send** — Send notifications at future times
- **Template Engine** — Inline or database-backed templates
- **OpenTelemetry Integration** — Full distributed tracing

See the [v0.3.0 implementation plan](../changelogs/v0.3.0-DASHBOARD-PLAN.md) for detailed build order and architecture.

---

## Migration from v0.1.0-beta.1

### Breaking Changes

**1. Package structure:**
- `RecurPixel.Notify.Core` and `RecurPixel.Notify.Orchestrator` are merged into `RecurPixel.Notify`
- Remove both old packages and install `RecurPixel.Notify` instead:

```bash
dotnet remove package RecurPixel.Notify.Core
dotnet remove package RecurPixel.Notify.Orchestrator
dotnet add package RecurPixel.Notify
```

**2. Namespace reorganization:**

Update your using statements:

| Old Namespace                             | New Namespace                                                                                      |
| ----------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `RecurPixel.Notify.Core.Models`           | `RecurPixel.Notify`                                                                                |
| `RecurPixel.Notify.Core.Channels`         | `RecurPixel.Notify.Channels`                                                                       |
| `RecurPixel.Notify.Core.Options`          | `RecurPixel.Notify` (core options) or `RecurPixel.Notify.Configuration` (channel/provider options) |
| `RecurPixel.Notify.Orchestrator.Services` | `RecurPixel.Notify`                                                                                |
| `RecurPixel.Notify.[Channel].[Provider]`  | `RecurPixel.Notify` (for ServiceCollectionExtensions)                                              |

**3. Return types:**

`TriggerAsync` now returns `TriggerResult` (not dynamic). `BulkTriggerAsync` returns `BulkTriggerResult`.

```csharp
// Before (v0.1.0-beta.1)
dynamic result = await notify.TriggerAsync(...);
if (result.Success) { ... }

// After (v0.2.0)
TriggerResult result = await notify.TriggerAsync(...);
if (result.AllSucceeded) { ... }  // Check all channels at once
foreach (var failure in result.Failures) { ... }  // Inspect per-channel failures
```

**4. InApp handler setup:**

The removal of `notifyOptions.InApp` and `notifyOptions.OnDelivery` properties is replaced with explicit calls:

```csharp
// Before (v0.1.0-beta.1)
notifyOptions.InApp = new() { /* ... */ };
notifyOptions.OnDelivery = async result => { /* ... */ };

// After (v0.2.0)
// ① InApp handler — separate call before AddRecurPixelNotify
builder.Services.AddInAppChannel(opts =>
    opts.UseHandler<IApplicationDbContext>(async (notification, db) => { /* ... */ }));

// ② Main registration
builder.Services.AddRecurPixelNotify(
    notifyOptions => { /* ... */ },
    orchestratorOptions =>
    {
        // ③ Delivery hook — inside AddRecurPixelNotify
        orchestratorOptions.OnDelivery<IApplicationDbContext>(async (result, db) => { /* ... */ });
    });
```

**Key distinction in v0.2.0:**
- **`UseHandler`** — where you implement the send (e.g., write InApp notifications to DB)
- **`OnDelivery`** — audit hook that fires after every send, for logging and metrics

### Code Update Checklist

- [ ] Remove `RecurPixel.Notify.Core` and `RecurPixel.Notify.Orchestrator` packages
- [ ] Add `RecurPixel.Notify` package
- [ ] Update `using RecurPixel.Notify.Core.*` → `using RecurPixel.Notify`
- [ ] Update `using RecurPixel.Notify.Core.Options.*` → `using RecurPixel.Notify.Configuration`
- [ ] Update `using RecurPixel.Notify.Core.Channels` → `using RecurPixel.Notify.Channels`
- [ ] Update `using RecurPixel.Notify.Orchestrator.Services` → `using RecurPixel.Notify`
- [ ] Move `notifyOptions.InApp` logic into `UseHandler<T>` call
- [ ] Move `notifyOptions.OnDelivery` logic into `OnDelivery<T>` call
- [ ] Update code that inspects `TriggerResult` (now strongly typed, not dynamic)

---

## License

MIT — see [LICENSE](https://github.com/RecurPixel/Notify/blob/main/LICENSE).
