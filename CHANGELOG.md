# Changelog

All notable changes to RecurPixel.Notify will be documented here.

---

## [0.1.0-beta.2] — Upcoming

### Bug Fixes
- [ ] **Simple channel registration key mismatch** — InApp, Slack, Discord, Teams, Telegram, Facebook, Line, Viber all register as `"channelname:channelname"` but the dispatcher resolves them as bare `"channelname"`, causing `InvalidOperationException` on every send
- [ ] **`IOptions<NotifyOptions>` not registered** — `AddRecurPixelNotify` calls `services.AddSingleton(options)` directly; `ChannelDispatcher` injects `IOptions<NotifyOptions>` and receives an empty default, causing null provider resolution
- [ ] **Misleading XML doc on `AddSmtpChannel`** — doc claims the method is called internally by `AddRecurPixelNotify()`; it is not

### Breaking Changes
- [ ] **`TriggerAsync` return type** — currently returns a single aggregated `NotifyResult`; will return a proper `TriggerResult` with `IReadOnlyList<NotifyResult> ChannelResults` for per-channel inspection
- [ ] **Package naming restructure** — `RecurPixel.Notify` (bare name) will become the base package (Core + Orchestrator); `RecurPixel.Notify.Sdk` remains as the all-adapters meta-package

### DX Improvements
- [ ] **Single setup call** — add `AddRecurPixelNotify(options, orchestratorConfigure)` overload to eliminate the two-call requirement
- [ ] **Silent no-send on channel name mismatch** — `UseChannels("email:smtp")` currently produces `Success = true` with nothing dispatched; will log a warning and surface a clear error
- [ ] **`OnDelivery` composable registrations** — calling `OnDelivery` twice silently drops the first handler; will support multiple registrations
- [ ] **`InApp.Handler` reference fragility** — handler wiring breaks if options are rebound after DI registration; will register handler as a separate keyed singleton
- [ ] **Simple channel registration keys** — the `"channel:provider"` key pattern only applies to multi-provider channels; simple channels use bare `"channelname"` keys; registration corrected and documented

### Documentation
- [ ] **Scoped services inside hooks** — document the `IServiceScopeFactory` pattern for accessing `DbContext` and other scoped services inside `OnDelivery` and `InApp.Handler`
- [ ] **`UseChannels()` accepts channel names, not provider names** — document with correct vs incorrect examples
- [ ] **`DefineEvent` required before `TriggerAsync`** — document requirement and add recommended `static class NotifyEvents` constants pattern
- [ ] **"Simple channel" vs "multi-provider channel"** — `GetChannelConfig` only handles email, sms, push, whatsapp; simple channels have no provider/fallback routing; document this distinction
- [ ] **Core + Orchestrator dependency** — document that `RecurPixel.Notify.Core` alone has no callable send API and that Orchestrator is required for `INotifyService`

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
