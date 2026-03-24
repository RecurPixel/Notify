# RecurPixel.Notify

A modular, DI-native NuGet notification library for ASP.NET Core. Drop it in. Bring your own API keys. Own your data.

[![NuGet](https://img.shields.io/nuget/v/RecurPixel.Notify.Sdk)](https://www.nuget.org/packages/RecurPixel.Notify.Sdk)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**✅ v0.2.0 STABLE** — Production-ready with 35+ adapters across 13+ channels

---

## What It Is

RecurPixel.Notify is a pure .NET library — not a platform, not SaaS, no external dependency. It handles multi-channel notification delivery (Email, SMS, Push, WhatsApp, Slack, Discord, Teams, Mattermost, Rocket.Chat, Telegram, Facebook, LINE, Viber, In-App) through a single consistent interface.

**You bring:** your API keys, your message content, your delivery log table.  
**We handle:** provider API calls, retry with exponential backoff, cross-channel fallback chains, parallel dispatch, and delivery hooks.

---

## Adapter Coverage & Test Matrix

All adapters are unit-tested. The table below shows which have integration testing (real API verification) and community usage. This matrix validates the **"stable" label** — you're not betting on guesses.

| Package                | Provider                          | Channel   | Unit Tested | Integration Tested | Community Approved |
| ---------------------- | --------------------------------- | --------- | ----------- | ------------------ | ------------------ |
| `Email.SendGrid`       | Twilio SendGrid                   | Email     | ✅           | ✅                  | 🔲                  |
| `Email.Smtp`           | Any SMTP server                   | Email     | ✅           | ✅                  | 🔲                  |
| `Email.Mailgun`        | Mailgun                           | Email     | ✅           | 🔲                  | 🔲                  |
| `Email.Resend`         | Resend                            | Email     | ✅           | ✅                  | 🔲                  |
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

## Install

```bash
# Full SDK — everything included
dotnet add package RecurPixel.Notify.Sdk

# Or install only what you need
dotnet add package RecurPixel.Notify
dotnet add package RecurPixel.Notify.Email.SendGrid
dotnet add package RecurPixel.Notify.Sms.Twilio
```

→ [Getting Started](https://recurpixel.github.io/Notify/getting-started) · [Quick Start](https://recurpixel.github.io/Notify/quick-start) · [Usage Tiers](https://recurpixel.github.io/Notify/usage-tiers) · [Adapter Reference](https://recurpixel.github.io/Notify/adapters)

> **⚠️ Upgrading from v0.1.0-beta.1?**
> v0.2.0 includes breaking changes: namespace reorganization, new meta-package structure, typed `TriggerResult` returns, and explicit `UseHandler` for InApp channels.
> **→ See the [Migration Guide](https://recurpixel.github.io/Notify#migration-from-v010-beta1)** for step-by-step upgrade instructions.

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

### Dashboard Package (`RecurPixel.Notify.Dashboard`)

A new observability package for delivery tracking and troubleshooting, built with a **data-first approach**: data layer solidified first, REST API second, UI last.

**Key Features:**
- **NotificationLog Entity & INotificationLogStore** — Pluggable persistence (SQL Server, PostgreSQL, SQLite, custom)
- **OnDelivery Hook Integration** — Logs automatically captured for every delivery
- **BulkBatchId Grouping** — Track all notifications in a bulk send as a unit (requires minor Orchestrator update to `BulkTriggerAsync`)
- **REST API** — Query logs, filter by channel/status/date, retrieve retry history, view batch history
- **Embedded HTML Dashboard** — Real-time logs table, channel status breakdown, failure histogram, live send testing

**Why This Order Matters:**
Dashboard development follows strict phases — UI is built last, not first. Data accuracy is verified before any visualization. See [v0.3.0 implementation plan](changelogs/v0.3.0-DASHBOARD-PLAN.md) for detailed build order.

### Adapter Improvements

- **Circuit Breaker Pattern** — Auto-disable broken channels without code changes
- **Community Adapter Approval** — Peer-reviewed providers earn 🟢 status
- **Adapter Analytics** — Success/failure rate tracking per provider (feeds Dashboard)

### Developer Experience

- **Scheduled Send** — Send notifications at future times
- **Template Engine** — Inline or database-backed notification templates
- **OpenTelemetry Integration** — Full distributed tracing for all channels

---

## License

MIT — see [LICENSE](LICENSE).