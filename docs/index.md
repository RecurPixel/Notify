---
layout: default
title: Home
nav_order: 1
permalink: /
---

# RecurPixel.Notify

A modular, DI-native NuGet notification library for ASP.NET Core. Drop it in. Bring your own API keys. Own your data.
{: .fs-6 .fw-300 }

**✅ v0.3.0 STABLE** — 39 packages across 14+ channels
{: .fs-5 }

[Get Started](getting-started){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[Quick Start](quick-start){: .btn .fs-5 .mb-4 .mb-md-0 }

> **Migrating from an older version?** See the [Migration Guide](migration).

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
| `Sms.Msg91`            | MSG91                             | SMS       | ✅           | ✅                  | 🔲                  |
| `Push.Fcm`             | Firebase Cloud Messaging          | Push      | ✅           | 🔲                  | 🔲                  |
| `Push.Apns`            | Apple Push Notification Service   | Push      | ✅           | 🔲                  | 🔲                  |
| `Push.OneSignal`       | OneSignal                         | Push      | ✅           | 🔲                  | 🔲                  |
| `Push.Expo`            | Expo Push                         | Push      | ✅           | 🔲                  | 🔲                  |
| `WhatsApp.Twilio`      | Twilio WhatsApp                   | WhatsApp  | ✅           | ✅                  | 🔲                  |
| `WhatsApp.MetaCloud`   | Meta Cloud API                    | WhatsApp  | ✅           | 🔲                  | 🔲                  |
| `WhatsApp.Vonage`      | Vonage WhatsApp                   | WhatsApp  | ✅           | 🔲                  | 🔲                  |
| `WhatsApp.Msg91`       | MSG91 WhatsApp Business           | WhatsApp  | ✅           | ✅                  | 🔲                  |
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
**Package count:** 39 (Core + Orchestrator + RecurPixel.Notify meta-package + 35 adapters + Dashboard + Dashboard.EfCore + Sdk)

---

## Design Principles

- **Zero infrastructure** — pure library, no platform to host or sign up for
- **Provider agnostic** — swap Twilio for Vonage with a config change, nothing else breaks
- **DI-native** — registers via `AddRecurPixelNotify()`, injected as `INotifyService`
- **Config agnostic** — accepts `IConfiguration`, options builder, or a raw POCO
- **Content agnostic** — we deliver the payload, you build the subject and body
- **Hook-based logging** — `OnDelivery()` callback, you write to your own DB

---

## What's New in v0.3.0

- **[Dashboard](dashboard)** — `RecurPixel.Notify.Dashboard` + `RecurPixel.Notify.Dashboard.EfCore`: delivery log UI, filterable table, batch drill-down, REST API, bring-your-own-store design
- **MSG91 Adapters** — `RecurPixel.Notify.Sms.Msg91` and `RecurPixel.Notify.WhatsApp.Msg91`
- **Richer `NotifyResult`** — new `EventName`, `BulkBatchId`, and `Subject` fields for full context in `OnDelivery` hooks
- **10 auto-registration fixes** — Twilio credential isolation, FCM/AwsSns/AwsSes/AzureComm crash fixes, HTTP named clients with timeouts on all adapters

**No breaking changes.** All v0.2.0 code compiles and runs without modification.

---

## Coming in v0.4.0

- **Polly Resilience Hooks** — expose `IHttpClientBuilder` per adapter so you attach your own Polly policies
- **OpenTelemetry Integration** — full distributed tracing via `ActivitySource`
- **Additional Adapters** — Kaleyra SMS, Gupshup WhatsApp, AiSensy WhatsApp
- **Dashboard v2** — batch detail page, provider health indicators, CSV export

See the [Roadmap](roadmap) for full details.

---

## License

MIT — see [LICENSE](https://github.com/RecurPixel/Notify/blob/main/LICENSE).
