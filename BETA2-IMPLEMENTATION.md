# RecurPixel.Notify — v0.1.0-beta.2 Implementation Record

> This document captures the agreed implementation plan for beta.2.
> It supersedes the earlier BETA2-PLAN.md checklist and incorporates the architectural decisions from ARCHITECTURE-DECISIONS.md.
> Use this as the authoritative work record during implementation.

---

## Background

Beta.1 integration testing in an ASP.NET Core project exposed five categories of issues:

1. **Hard crashes** — simple channels registered as `"channel:channel"` but dispatcher resolves `"channel:default"`; `IOptions<NotifyOptions>` not registered so ChannelDispatcher always reads an empty options object
2. **Silent failures** — `OnDelivery` silently overwrites on second call; `UseChannels("email:smtp")` dispatches nothing but returns `Success=true`
3. **Bad DX** — two separate setup calls required; no per-channel result inspection from `TriggerAsync`
4. **Wrong architecture** — explicit per-adapter registration calls break the appsettings-driven config philosophy
5. **Package confusion** — `RecurPixel.Notify` doesn't exist on NuGet; Core + Orchestrator split is invisible noise to users

---

## Key Architectural Decisions

| Decision | Choice |
|----------|--------|
| Package structure | `RecurPixel.Notify` = meta-package (Core + Orchestrator). Adapters stay on `RecurPixel.Notify.Core`. Clean dependency graph. |
| Auto-registration | Assembly scanning via `[ChannelAdapter]` attribute + `AppDomain`. Orchestrator's `AddRecurPixelNotify` discovers and registers adapters at startup. |
| Config-based filtering | An adapter is **only registered in DI if its minimum required credential is present in config**. No config = adapter does not exist = no broken registrations. |
| `Add{X}Channel()` methods | Retained for Tier 1 direct-injection. Changed to `TryAddKeyedSingleton` (idempotent). XML docs updated. |
| `TriggerAsync` return type | `Task<TriggerResult>` with `IReadOnlyList<NotifyResult> ChannelResults` and `string? UserId` |
| `BulkTriggerAsync` return type | `Task<BulkTriggerResult>` with `IReadOnlyList<TriggerResult> Results` — one per input context |
| `BulkNotifyResult` | **Kept** — correct shape for direct channel bulk sends (`channel.SendBulkAsync`). Unrelated to orchestrated bulk. |
| InApp | Scanner registers `InAppChannel` (`"inapp:default"`). `AddInAppChannel` wires delivery handler. Missing handler = graceful `NotifyResult { Success=false }`, not a crash. |

---

## Phase 0 — Implementation Record ✅

Create this file.

---

## Phase 1 — Fix Blocking Bugs

### 1a. Fix `"channel:default"` key for all simple channels

Simple channels are registered as `"channel:channel"` but dispatcher falls through to `"channel:default"`. Hard crash on every send.

**Fix:** Update `ServiceCollectionExtensions.cs` in each simple channel package:

| Package | Old Key | New Key |
|---------|---------|---------|
| `RecurPixel.Notify.Slack` | `"slack:slack"` | `"slack:default"` |
| `RecurPixel.Notify.Discord` | `"discord:discord"` | `"discord:default"` |
| `RecurPixel.Notify.Teams` | `"teams:teams"` | `"teams:default"` |
| `RecurPixel.Notify.Telegram` | `"telegram:telegram"` | `"telegram:default"` |
| `RecurPixel.Notify.Facebook` | `"facebook:facebook"` | `"facebook:default"` |
| `RecurPixel.Notify.Line` | `"line:line"` | `"line:default"` |
| `RecurPixel.Notify.Viber` | `"viber:viber"` | `"viber:default"` |
| `RecurPixel.Notify.Mattermost` | `"mattermost:mattermost"` | `"mattermost:default"` |
| `RecurPixel.Notify.RocketChat` | `"rocketchat:rocketchat"` | `"rocketchat:default"` |
| `RecurPixel.Notify.InApp` | `"inapp:inapp"` | `"inapp:default"` |

**Also fix:** `ChannelDispatcher.GetChannelConfig` — ensure all simple channels are explicitly listed with `(null, null, null)` and the catch-all `_` throws `InvalidOperationException`.

- [ ] Update all 10 simple channel ServiceCollectionExtensions.cs
- [ ] Update ChannelDispatcher.GetChannelConfig

### 1b. Fix `IOptions<NotifyOptions>` registration

`ChannelDispatcher` injects `IOptions<NotifyOptions>` but only raw `NotifyOptions` is registered. Fix: register both.

```csharp
services.AddSingleton<IOptions<NotifyOptions>>(Options.Create(options));
services.AddSingleton(options);
```

- [ ] Fix `src/RecurPixel.Notify.Core/Extensions/ServiceCollectionExtensions.cs`

### 1c. Fix composable `OnDelivery`

Calling `OnDelivery` twice silently drops the first handler. Fix: internal `List<>`, all handlers fire.

- [ ] Update `OrchestratorOptions` — replace `DeliveryHook` with `_deliveryHandlers` list + `InvokeDeliveryHandlers`
- [ ] Update `ChannelDispatcher` — call `InvokeDeliveryHandlers` instead of `DeliveryHook?.Invoke`

---

## Phase 2 — `RecurPixel.Notify` Meta-Package

**Dependency graph after this change:**
```
RecurPixel.Notify.Core          → interfaces, models, options
RecurPixel.Notify.Orchestrator  → dispatch, events, NotifyService (depends on Core)
RecurPixel.Notify               → meta-package: Core + Orchestrator (no code)
RecurPixel.Notify.Sdk           → meta-package: RecurPixel.Notify + all adapters
All adapter packages             → RecurPixel.Notify.Core (unchanged)
```

- [ ] Create `src/RecurPixel.Notify/RecurPixel.Notify.csproj` (pure meta-package, no .cs files)
- [ ] Update `RecurPixel.Notify.Sdk.csproj` to reference `RecurPixel.Notify` meta-package
- [ ] Add to solution file

Migration for beta.1 users:
```bash
dotnet remove package RecurPixel.Notify.Core
dotnet remove package RecurPixel.Notify.Orchestrator
dotnet add package RecurPixel.Notify
```

---

## Phase 3 — Auto-Registration Architecture

### 3a. `[ChannelAdapter]` attribute in Core

New file: `src/RecurPixel.Notify.Core/Channels/ChannelAdapterAttribute.cs`

- [ ] Create attribute class

### 3b. Apply attribute to all 30+ adapter classes

| Group | Adapters |
|-------|----------|
| Email ×7 | SendGrid, Smtp, Mailgun, Resend, Postmark, AwsSes, AzureCommEmail |
| Sms ×7 | Twilio, Vonage, Plivo, Sinch, MessageBird, AwsSns, AzureCommSms |
| Push ×4 | Fcm, Apns, OneSignal, Expo |
| WhatsApp ×3 | Twilio, MetaCloud, Vonage |
| Simple ×10 | Slack, Discord, Teams, Telegram, Facebook, Line, Viber, Mattermost, RocketChat, InApp |

Multi-provider: `[ChannelAdapter("email", "sendgrid")]`
Simple: `[ChannelAdapter("slack", "default")]`

- [ ] Apply to all 31 adapter classes

### 3c. Scanner + Config Filter in Orchestrator

**File:** `src/RecurPixel.Notify.Orchestrator/Extensions/ServiceCollectionExtensions.cs`

**Safety rule:** An adapter is only registered in DI if its minimum required credential is present in config. No config = adapter does not exist.

Three methods to implement:

1. **`DiscoverAdapters()`** — `AppDomain` scan with `ReflectionTypeLoadException` guard
2. **`IsAdapterConfigured(NotifyOptions, channel, provider)`** — credential presence check per provider
3. **`RegisterAdapters()`** — `TryAddKeyedSingleton` for passing adapters, returns `registeredKeys`
4. **`ValidateActiveProviders()`** — fail-fast check that `Provider` values have matching registrations

`IsAdapterConfigured` credential checks:
- `email:sendgrid` → `SendGrid.ApiKey is not null`
- `email:smtp` → `Smtp.Host is not null`
- `email:mailgun` → `Mailgun.ApiKey is not null`
- `email:resend` → `Resend.ApiKey is not null`
- `email:postmark` → `Postmark.ApiKey is not null`
- `email:awsses` → `AwsSes.AccessKey is not null`
- `email:azurecommemail` → `AzureCommEmail.ConnectionString is not null`
- `sms:twilio` → `Twilio.AccountSid is not null`
- `sms:vonage` → `Vonage.ApiKey is not null`
- `sms:plivo` → `Plivo.AuthId is not null`
- `sms:sinch` → `Sinch.ApiKey is not null`
- `sms:messagebird` → `MessageBird.ApiKey is not null`
- `sms:awssns` → `AwsSns.AccessKey is not null`
- `sms:azurecommsms` → `AzureCommSms.ConnectionString is not null`
- `push:fcm` → `Fcm.ProjectId is not null`
- `push:apns` → `Apns.KeyId is not null`
- `push:onesignal` → `OneSignal.AppId is not null`
- `push:expo` → `Expo.AccessToken is not null`
- `whatsapp:twilio` → `WhatsApp.Twilio.AccountSid is not null`
- `whatsapp:metacloud` → `WhatsApp.MetaCloud.PhoneNumberId is not null`
- `whatsapp:vonage` → `WhatsApp.Vonage.ApiKey is not null`
- `slack` → `Slack.WebhookUrl is not null`
- `discord` → `Discord.WebhookUrl is not null`
- `teams` → `Teams.WebhookUrl is not null`
- `telegram` → `Telegram.BotToken is not null`
- `facebook` → `Facebook.PageAccessToken is not null`
- `line` → `Line.ChannelAccessToken is not null`
- `viber` → `Viber.AuthToken is not null`
- `mattermost` → `Mattermost.WebhookUrl is not null`
- `rocketchat` → `RocketChat.WebhookUrl is not null`
- `inapp` → always `true`

- [ ] Implement `DiscoverAdapters()`
- [ ] Implement `IsAdapterConfigured()`
- [ ] Implement `RegisterAdapters()`
- [ ] Implement `ValidateActiveProviders()`

### 3d. Update `Add{X}Channel()` methods

Change `AddKeyedSingleton` → `TryAddKeyedSingleton` in all 31 adapter `ServiceCollectionExtensions.cs`.
Update XML docs to remove "called internally" language.

- [ ] Update all 31 methods

---

## Phase 4 — `TriggerResult` + `BulkTriggerResult`

### New models

**`TriggerResult`** — per-event, per-channel breakdown:
```csharp
public string EventName { get; init; }
public string? UserId { get; init; }   // from NotifyContext.User.UserId for bulk correlation
public IReadOnlyList<NotifyResult> ChannelResults { get; init; }
public bool AllSucceeded / AnySucceeded / Failures { ... }
```

**`BulkTriggerResult`** — one `TriggerResult` per input context:
```csharp
public IReadOnlyList<TriggerResult> Results { get; init; }
public AllSucceeded / AnySucceeded / Total / SuccessCount / FailureCount / Failures { ... }
```

**`BulkNotifyResult` stays** — correct shape for `channel.SendBulkAsync()`. Not changed.

- [ ] Create `TriggerResult.cs` in Orchestrator
- [ ] Create `BulkTriggerResult.cs` in Orchestrator
- [ ] Update `INotifyService.TriggerAsync` → `Task<TriggerResult>`
- [ ] Update `INotifyService.BulkTriggerAsync` → `Task<BulkTriggerResult>`
- [ ] Update `NotifyService` implementations

---

## Phase 5 — DX Improvements

### 5a. Combined setup overloads

Two new `AddRecurPixelNotify` overloads in Orchestrator's `ServiceCollectionExtensions` that wire Core + scanner + Orchestrator in one call:
- `AddRecurPixelNotify(IConfiguration configSection, Action<OrchestratorOptions> configure)`
- `AddRecurPixelNotify(Action<NotifyOptions> configureOptions, Action<OrchestratorOptions> configureOrchestrator)`

- [ ] Add both overloads

### 5b. `OnDelivery<TService>` typed overload

Creates scope internally, resolves `TService`, calls handler. User just receives the service.

- [ ] Add typed overload to `OrchestratorOptions`

### 5c. `InApp.OnDeliver` + `InAppNotification`

- New `InAppNotification` model (UserId, Subject, Body, Metadata)
- `InAppOptions.OnDeliver<TService>` typed overload with internal scope management
- `InAppChannel.SendAsync` maps `NotificationPayload` → `InAppNotification`, calls `DeliverHandler`, returns graceful error if handler not configured
- `IOptions<InAppOptions>` registered by scanner (empty default); `AddInAppChannel` wires the handler

- [ ] Create `InAppNotification.cs`
- [ ] Update `InAppOptions.cs`
- [ ] Update `InAppChannel.cs`
- [ ] Update `AddInAppChannel` extension

### 5d. Silent no-send detection

In `NotifyService` dispatch loop: if `context.Channels` has no entry for a channel listed in `UseChannels`, log `LogWarning` and add a `NotifyResult { Success=false }` entry. Never silently succeed.

- [ ] Add check in `NotifyService` dispatch loop

---

## Phase 6 — Documentation

- [ ] Update XML docs on all `Add{X}Channel()` extension methods
- [ ] Add README sections: Simple vs Multi-Provider channels; scoped services; `UseChannels` keys; `NotifyEvents` constants; beta.1 → beta.2 migration
- [ ] Update CHANGELOG.md — move planned → fixed

---

## Implementation Checklist (flat)

### Blocking Bugs
- [ ] Update 10 simple channel `ServiceCollectionExtensions.cs` — key `"channel:default"`
- [ ] Update `ChannelDispatcher.GetChannelConfig` — cover all simple channels
- [ ] Fix `IOptions<NotifyOptions>` registration in `AddRecurPixelNotify`
- [ ] Replace `OrchestratorOptions.DeliveryHook` with `_deliveryHandlers` list
- [ ] Update `ChannelDispatcher` to call `InvokeDeliveryHandlers`

### Package Restructure
- [ ] Create `src/RecurPixel.Notify/RecurPixel.Notify.csproj` (meta-package)
- [ ] Update `RecurPixel.Notify.Sdk.csproj`
- [ ] Update solution file

### Auto-Registration
- [ ] Create `ChannelAdapterAttribute.cs` in Core
- [ ] Apply `[ChannelAdapter]` to all 31 adapter classes
- [ ] Implement `DiscoverAdapters()`, `IsAdapterConfigured()`, `RegisterAdapters()`, `ValidateActiveProviders()`
- [ ] Change `AddKeyedSingleton` → `TryAddKeyedSingleton` in all 31 adapter extensions

### API Changes
- [ ] Create `TriggerResult.cs`
- [ ] Create `BulkTriggerResult.cs`
- [ ] Update `INotifyService` + `NotifyService` (both async method return types)

### DX
- [ ] Add combined `AddRecurPixelNotify` overloads (×2)
- [ ] Add `OnDelivery<TService>` typed overload
- [ ] Create `InAppNotification.cs`, update `InAppOptions.cs` + `InAppChannel.cs` + `AddInAppChannel`
- [ ] Add silent no-send detection in dispatch loop

### Documentation
- [ ] XML docs on all `Add{X}Channel()` methods
- [ ] README sections
- [ ] CHANGELOG.md

---

*RecurPixel.Notify — v0.1.0-beta.2 Implementation Record. March 2026.*
