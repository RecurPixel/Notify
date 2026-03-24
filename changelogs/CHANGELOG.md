# Changelog

All notable changes to RecurPixel.Notify will be documented here.

---

## [0.2.0] — March 2026 — STABLE

**🎉 STABLE RELEASE — Production Ready**

The v0.2.0 stable release brings a fully restructured, production-ready notification library with 35+ adapter packages across 13+ channels and providers.

### Adapter Maturity Matrix

This table reflects the testing and verification status of each adapter in v0.2.0. All adapters are included in the stable release. The matrix **validates the "stable" label** — unit tests confirm core logic, integration tests verify provider APIs for adapters marked ✅, and community-backed adapters provide real-world usage assurance.

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

### What's New in v0.2.0

- **21 new or majorly restructured packages** — complete channel expansion
- **Backward-incompatible improvements** — API refined for production patterns
- **Zero SaaS platform dependency** — pure .NET library, full control
- **Auto-discovery of adapters** — install a package, configure credentials, go
- **DI-native setup** — single `AddRecurPixelNotify()` call
- **Delivery hooks** — custom logging to your own database
- **Retry with backoff** — built-in resilience
- **Fallback chains** — automatic failover between providers
- **Scoped service support** — `DbContext` and scoped dependencies work natively

### Breaking Changes from v0.2.0-beta.1/beta.2

**See [v0.2.0-beta.1](#020-beta1--march-2026) for complete breaking change documentation.**

Key differences:
- `TriggerAsync` now returns `TriggerResult` with typed channel results
- Package namespace reorganization (Core models moved to root namespace)
- InApp `OnDeliver` renamed to `UseHandler`
- Meta-package structure (install `RecurPixel.Notify` + adapters instead of separate Core + Orchestrator)

### What's Next: v0.3.0 Roadmap

We're committing to a structured v0.3.0 release with three major feature areas and a new Dashboard package:

#### 1. **Dashboard Package** (`RecurPixel.Notify.Dashboard`) — Delivery Tracking & Observability

**Build Order (Data-First Approach):**

The dashboard is implemented in phases. **UI is built last**, not first, ensuring data accuracy before visualization:

**Phase 1: Data Layer (Foundation)**
- `NotificationLog` entity — stores delivery attempts, results, retry context, provider responses
- `INotificationLogStore` interface — abstraction for log persistence (SQL, NoSQL, file, etc.)
- Wire `OnDelivery` hook into core Orchestrator — calls `INotificationLogStore.LogDeliveryAsync()`
- Verify logs are actually being written correctly — unit tests for store implementations

**Phase 2: API Layer (Backend)**
- JSON REST endpoints for log queries, filtering by date/channel/status, aggregations
- Batcher support — delivery history per `BulkBatchId`
- Performance queries (built-in pagination, indexing guidance)

**Phase 3: UI Layer (Client)**
- Embedded HTML dashboard — runs in your app's auth context
- Real-time delivery logs table, channel status breakdown, failure histogram
- Retry history and per-message provider responses
- **Why last:** UI is only useful if the data layer is solid

**Critical Change in Orchestrator:**
- `BulkTriggerAsync` generates a `BulkBatchId` (ULID/Guid) and passes it through to each individual `NotifyResult`
- Minimal, isolated change: only `BulkTriggerAsync` and `NotifyResult` touched
- Prototype this early (before UI work) to validate the plumbing

#### 2. **Adapter Improvements**

- **Community Adapter Approval Process** — peer-reviewed providers to earn 🟢 status
- **Circuit Breaker Pattern** — auto-disable broken channels without code changes
- **Adapter Analytics** — built-in success/failure rate tracking (feeds Dashboard)

#### 3. **Developer Experience**

- **Scheduled Send** — send notifications at future times
- **Template Engine** — inline or DB-backed notification templates
- **OpenTelemetry Integration** — full tracing for all channels

---

## v0.2.0-beta.2 — CI Fix

This release includes all v0.2.0-beta.1 changes (see [v0.2.0-beta.1](#020-beta1--march-2026)) plus:

### Fixes
- Fixed GitHub Actions integration tests configuration loading
- `appsettings.integration.json` is now optional in CI — tests gracefully skip when credentials are missing

**Status:** Ready for NuGet publication

---

## [0.2.0-beta.1] — March 2026

### New Packages
- `RecurPixel.Notify` — merged meta-package replacing the separate Core + Orchestrator install
- `RecurPixel.Notify.Email.AzureCommEmail`
- `RecurPixel.Notify.Sms.AzureCommSms`
- `RecurPixel.Notify.Mattermost`
- `RecurPixel.Notify.RocketChat`

### Breaking Changes
- **`TriggerAsync` return type** — now returns `TriggerResult` with `IReadOnlyList<NotifyResult> ChannelResults`, `AllSucceeded`, `AnySucceeded`, and `Failures` for per-channel inspection
- **`BulkTriggerAsync` return type** — now returns `BulkTriggerResult` with one `TriggerResult` per input context
- **`TriggerResult` / `BulkTriggerResult` moved to Core** — namespace is now `RecurPixel.Notify.Core.Models`
- **Package restructure** — `RecurPixel.Notify` is now the primary install (Core + Orchestrator). Users of beta.1 should remove `RecurPixel.Notify.Core` and `RecurPixel.Notify.Orchestrator` and add `RecurPixel.Notify`
- **Core setup method renamed** — `AddRecurPixelNotify` in Core renamed to `AddNotifyOptions` to avoid confusion with the Orchestrator overload
- **`InAppOptions.OnDeliver` renamed to `UseHandler`** — aligns with the handler-as-implementation pattern
- **Dead properties removed** — `NotifyOptions.OnDelivery` and `NotifyOptions.InApp` removed; use `OrchestratorOptions.OnDelivery` and `AddInAppChannel` respectively
- **Namespace reorganization** — Major cleanup for cleaner public API surface:
  - Models (`NotificationPayload`, `NotifyResult`, `BulkNotifyResult`, `NotifyContext`, `NotifyUser`) moved from `RecurPixel.Notify.Core.Models` → `RecurPixel.Notify`
  - Channel interfaces and bases (`INotificationChannel`, `NotificationChannelBase`, `ChannelAdapterAttribute`) moved from `RecurPixel.Notify.Core.Channels` → `RecurPixel.Notify.Channels`
  - Core options (`NotifyOptions`, `RetryOptions`, `FallbackOptions`, `BulkOptions`) moved from `RecurPixel.Notify.Core.Options` → `RecurPixel.Notify`
  - Channel-specific options (`EmailOptions`, `SmsOptions`, `PushOptions`, `WhatsAppOptions`) and all provider credential options moved from `RecurPixel.Notify.Core.Options.*` → `RecurPixel.Notify.Configuration`
  - All public channel implementation classes moved to `RecurPixel.Notify.Channels` (e.g., `SendGridChannel`, `SlackChannel`, `TwilioSmsChannel`, etc.)
  - Services (`INotifyService`, `NotifyService`) moved from `RecurPixel.Notify.Orchestrator.Services` → `RecurPixel.Notify`
  - All adapter `ServiceCollectionExtensions` moved from `RecurPixel.Notify.[Channel].[Provider]` → `RecurPixel.Notify`

### Bug Fixes
- **Simple channel registration key mismatch** — all simple channels (InApp, Slack, Discord, Teams, Telegram, Facebook, Line, Viber, Mattermost, RocketChat) now register and resolve as `"channel:default"`
- **`IOptions<NotifyOptions>` not registered** — both raw `NotifyOptions` POCO and `IOptions<NotifyOptions>` are now registered; `ChannelDispatcher` receives the correct options
- **Adapter credentials not reaching DI** — `ConfigureAllKnownOptions` now maps every provider's credentials from the `NotifyOptions` POCO into `IOptions<TAdapterOptions>` at startup; adapters constructed by DI receive real credentials instead of empty defaults
- **Telegram `ChatId` fallback** — `TelegramChannel` now falls back to `TelegramOptions.ChatId` when `payload.To` is empty, with a clear error if neither is set
- **Misleading XML doc on `AddSmtpChannel`** — corrected; the method is not called internally by `AddRecurPixelNotify`

### DX Improvements
- **Single setup call** — `AddRecurPixelNotify(Action<NotifyOptions>, Action<OrchestratorOptions>)` combines options binding, adapter auto-registration, and orchestrator setup in one call
- **Auto-registration via assembly scanning** — install a provider package and configure its credentials; no `Add{X}Channel()` call required. `[ChannelAdapter]` attribute drives discovery at startup
- **Config-filtered registration** — only adapters with credentials present in `NotifyOptions` are registered; unconfigured providers have zero DI footprint
- **Startup validation** — `ValidateActiveProviders` throws `InvalidOperationException` at startup when `Provider` is set but credentials are missing — never silently at send time
- **`OnDelivery` composable** — multiple `OnDelivery` / `OnDelivery<TService>` registrations are additive; all handlers fire in registration order
- **`OnDelivery<TService>` scoped resolution** — creates a fresh DI scope per call; `DbContext` and other scoped services are safe to use directly without `IServiceScopeFactory`
- **`AddInAppChannel` + `UseHandler<TService>`** — InApp delivery wired via a typed scoped handler; `OnDelivery` remains a separate audit hook
- **Silent no-send warning** — `NotifyService` logs a warning when a channel is in the event definition but has no payload in `NotifyContext.Channels`
- **Improved `ResolveAdapter` error** — multi-cause diagnostic message when a channel adapter key is not found in DI
- **`INotifyService` channel properties** — added `Line`, `Viber`, `Mattermost`, `RocketChat` direct channel properties
- **All adapter extensions use `TryAddKeyedSingleton`** — idempotent; explicit `Add{X}Channel()` calls and auto-registration no longer conflict

### Documentation
- **All docs updated** — `getting-started.md`, `usage-tiers.md`, `quick-start.md`, `features.md` rewritten to reflect current API
- **New `examples.md`** — Tier 1 (single-channel OTP), Tier 2 (e-commerce), Tier 3 (full SDK), and a production-ready pattern with InApp + OnDelivery + event definitions
- **Scoped services pattern documented** — `OnDelivery<TService>` and `UseHandler<TService>` DI scope behaviour explained
- **`UseChannels` key names documented** — logical channel names only (`"email"`, `"sms"`), not provider names

---

## [0.1.0-beta.1] — February 2026

Initial beta release.

### Packages Published
- `RecurPixel.Notify.Core`
- `RecurPixel.Notify.Orchestrator`
- `RecurPixel.Notify.Email.SendGrid`
- `RecurPixel.Notify.Email.Smtp`
- `RecurPixel.Notify.Email.Mailgun`
- `RecurPixel.Notify.Email.Resend`
- `RecurPixel.Notify.Email.Postmark`
- `RecurPixel.Notify.Email.AwsSes`
- `RecurPixel.Notify.Sms.Twilio`
- `RecurPixel.Notify.Sms.Vonage`
- `RecurPixel.Notify.Sms.Plivo`
- `RecurPixel.Notify.Sms.Sinch`
- `RecurPixel.Notify.Sms.MessageBird`
- `RecurPixel.Notify.Sms.AwsSns`
- `RecurPixel.Notify.Push.Fcm`
- `RecurPixel.Notify.Push.Apns`
- `RecurPixel.Notify.Push.OneSignal`
- `RecurPixel.Notify.Push.Expo`
- `RecurPixel.Notify.WhatsApp.Twilio`
- `RecurPixel.Notify.WhatsApp.MetaCloud`
- `RecurPixel.Notify.WhatsApp.Vonage`
- `RecurPixel.Notify.Slack`
- `RecurPixel.Notify.Discord`
- `RecurPixel.Notify.Teams`
- `RecurPixel.Notify.Telegram`
- `RecurPixel.Notify.Facebook`
- `RecurPixel.Notify.Line`
- `RecurPixel.Notify.Viber`
- `RecurPixel.Notify.InApp`
- `RecurPixel.Notify.Sdk`

### Features
- Multi-channel notification delivery — Email, SMS, Push, WhatsApp, Slack, Discord, Teams, Telegram, Facebook, LINE, Viber, InApp
- Orchestrator with event registry, conditions, parallel dispatch, retry with exponential backoff, cross-channel fallback chains
- Multi-provider support per channel — named routing via `Metadata["provider"]`, within-channel fallback
- Bulk send — `BulkTriggerAsync` for multi-user dispatch; native batch API support for FCM, SendGrid, Postmark, Mailgun, AwsSes, Vonage, Sinch, OneSignal, Expo
- Delivery hook — `OnDelivery` callback for user-owned notification logging
- Direct send path — bypass orchestration for time-critical flows
- Three install tiers — single adapter, Core + Orchestrator, or full SDK meta-package

### Known Issues
See the beta.2 planned fixes above for the full list of confirmed issues discovered during initial integration testing.
