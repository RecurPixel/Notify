# RecurPixel.Notify

A modular, DI-native NuGet notification library for ASP.NET Core. Drop it in. Bring your own API keys. Own your data.

[![NuGet](https://img.shields.io/nuget/v/RecurPixel.Notify.Sdk)](https://www.nuget.org/packages/RecurPixel.Notify.Sdk)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

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
dotnet add package RecurPixel.Notify.Core
dotnet add package RecurPixel.Notify.Orchestrator
dotnet add package RecurPixel.Notify.Email.SendGrid
dotnet add package RecurPixel.Notify.Sms.Twilio
```

→ [Getting Started](docs/getting-started.md) · [Quick Start](docs/quick-start.md) · [Usage Tiers](docs/usage-tiers.md) · [Adapter Reference](docs/adapters.md)

---

## Adapter Status

| Package              | Provider                          | Channel   | Unit Tested | Integration Tested | Community Approved |
| -------------------- | --------------------------------- | --------- | ----------- | ------------------ | ------------------ |
| `Email.SendGrid`     | Twilio SendGrid                   | Email     | ✅           | ✅                  | 🔲                  |
| `Email.Smtp`         | Any SMTP server                   | Email     | ✅           | ✅                  | 🔲                  |
| `Email.Mailgun`      | Mailgun                           | Email     | ✅           | 🔲                  | 🔲                  |
| `Email.Resend`       | Resend                            | Email     | ✅           | 🔲                  | 🔲                  |
| `Email.Postmark`     | Postmark                          | Email     | ✅           | 🔲                  | 🔲                  |
| `Email.AwsSes`       | AWS SES                           | Email     | ✅           | 🔲                  | 🔲                  |
| `Email.AzureCommEmail` | Azure Communication Services    | Email     | ✅           | 🔲                  | 🔲                  |
| `Sms.Twilio`         | Twilio                            | SMS       | ✅           | ✅                  | 🔲                  |
| `Sms.Vonage`         | Vonage (Nexmo)                    | SMS       | ✅           | 🔲                  | 🔲                  |
| `Sms.Plivo`          | Plivo                             | SMS       | ✅           | 🔲                  | 🔲                  |
| `Sms.Sinch`          | Sinch                             | SMS       | ✅           | 🔲                  | 🔲                  |
| `Sms.MessageBird`    | MessageBird                       | SMS       | ✅           | 🔲                  | 🔲                  |
| `Sms.AwsSns`         | AWS SNS                           | SMS       | ✅           | 🔲                  | 🔲                  |
| `Sms.AzureCommSms`   | Azure Communication Services      | SMS       | ✅           | 🔲                  | 🔲                  |
| `Push.Fcm`           | Firebase Cloud Messaging          | Push      | ✅           | 🔲                  | 🔲                  |
| `Push.Apns`          | Apple Push Notification Service   | Push      | ✅           | 🔲                  | 🔲                  |
| `Push.OneSignal`     | OneSignal                         | Push      | ✅           | 🔲                  | 🔲                  |
| `Push.Expo`          | Expo Push                         | Push      | ✅           | 🔲                  | 🔲                  |
| `WhatsApp.Twilio`    | Twilio WhatsApp                   | WhatsApp  | ✅           | ✅                  | 🔲                  |
| `WhatsApp.MetaCloud` | Meta Cloud API                    | WhatsApp  | ✅           | 🔲                  | 🔲                  |
| `WhatsApp.Vonage`    | Vonage WhatsApp                   | WhatsApp  | ✅           | 🔲                  | 🔲                  |
| `Slack`              | Slack Webhooks / Bot API          | Team Chat | ✅           | ✅                  | 🔲                  |
| `Discord`            | Discord Webhooks                  | Team Chat | ✅           | ✅                  | 🔲                  |
| `Teams`              | Microsoft Teams Webhooks          | Team Chat | ✅           | 🔲                  | 🔲                  |
| `Mattermost`         | Mattermost Webhooks               | Team Chat | ✅           | 🔲                  | 🔲                  |
| `RocketChat`         | Rocket.Chat Webhooks              | Team Chat | ✅           | 🔲                  | 🔲                  |
| `Facebook`           | Meta Messenger API                | Social    | ✅           | 🔲                  | 🔲                  |
| `Telegram`           | Telegram Bot API                  | Social    | ✅           | ✅                  | 🔲                  |
| `Line`               | LINE Messaging API                | Social    | ✅           | 🔲                  | 🔲                  |
| `Viber`              | Viber Business Messages           | Social    | ✅           | 🔲                  | 🔲                  |
| `InApp`              | Hook-based (user-defined storage) | In-App    | ✅           | ✅                  | 🔲                  |

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

## License

MIT — see [LICENSE](LICENSE).