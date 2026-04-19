# RecurPixel.Notify — Build Progress Tracker

> Keep this file in the repo root. Update the checkboxes as you complete each step.
> Start a new chat per Phase to keep context clean. Paste the Phase heading into the chat so Claude knows exactly where you are.

---

## How to Use This File

- ✅ = Done
- 🔲 = Not started
- 🔧 = In progress
- Each Phase = one focused chat session
- If you hit a usage limit mid-phase, the **Context Prompt** at the bottom of each phase lets you resume cold

---

## Phase 1 — Core Library `RecurPixel.Notify.Core` ✅

> Interfaces, models, options, base class, DI wiring. Everything else depends on this.

- ✅ Solution + project scaffold
- ✅ Folder structure (`Channels`, `Models`, `Options`, `Extensions`)
- ✅ `IsExternalInit.cs` shim
- ✅ `NotificationPayload.cs`
- ✅ `NotifyResult.cs`
- ✅ `BulkNotifyResult.cs`
- ✅ `NotifyUser.cs`
- ✅ `NotifyContext.cs`
- ✅ `INotificationChannel.cs`
- ✅ `NotificationChannelBase.cs`
- ✅ `BulkOptions.cs`
- ✅ `RetryOptions.cs`
- ✅ `FallbackOptions.cs`
- ✅ `NamedProviderDefinition.cs`
- ✅ `EmailProviderOptions.cs` (SendGrid, Smtp, Mailgun, Resend, Postmark, AwsSes)
- ✅ `SmsProviderOptions.cs` (Twilio, Vonage, Plivo, Sinch, MessageBird, AwsSns)
- ✅ `PushProviderOptions.cs` (Fcm, Apns, OneSignal, Expo)
- ✅ `MessagingProviderOptions.cs` (MetaCloud, Slack, Discord, Teams, Telegram, Facebook)
- ✅ `EmailOptions.cs`
- ✅ `SmsOptions.cs`
- ✅ `PushOptions.cs`
- ✅ `WhatsAppOptions.cs`
- ✅ `NotifyOptions.cs`
- ✅ `ServiceCollectionExtensions.cs`
- ✅ `NotifyOptionsValidator.cs`
- ✅ `dotnet build` — clean

---

## Phase 2 — Test Project + First Two Adapters ✅

- ✅ `tests/RecurPixel.Notify.Tests` xUnit project
- ✅ Core contract tests
- ✅ `Email.SendGrid` adapter + unit tests
- ✅ `Email.Smtp` adapter + unit tests
- ✅ `dotnet test` — all green

---

## Phase 3 — SMS Adapter `Sms.Twilio` ✅

- ✅ `TwilioSmsChannel : NotificationChannelBase`
- ✅ Unit tests
- ✅ `dotnet test` — all green

---

## Phase 4 — Orchestrator `RecurPixel.Notify.Orchestrator` ✅

- ✅ `EventDefinition`, `EventRegistry`
- ✅ `INotifyService`, `NotifyService`
- ✅ `TriggerAsync`, `BulkTriggerAsync`
- ✅ Parallel dispatch, condition evaluation
- ✅ Multi-provider resolution, keyed DI
- ✅ Delivery hook wiring
- ✅ Unit tests
- ✅ `dotnet test` — all green

---

## Phase 5 — Retry + Fallback ✅

- ✅ Retry policy engine (MaxAttempts, DelayMs, ExponentialBackoff)
- ✅ Per-event retry override
- ✅ Cross-channel fallback chain
- ✅ Per-event fallback override
- ✅ Unit tests
- ✅ `dotnet test` — all green

---

## Phase 6 — Delivery Hook + ILogger ✅

- ✅ `OnDelivery` callback
- ✅ `ILogger<T>` structured logging in adapters and Orchestrator
- ✅ `dotnet test` — all green

---

## Phase 7 — Push Adapters ✅

- ✅ `Push.Fcm` (SendBulkAsync override — 500 tokens/multicast)
- ✅ `Push.Apns`
- ✅ Tests
- ✅ `dotnet test` — all green

---

## Phase 8 — WhatsApp Adapters ✅

- ✅ `WhatsApp.Twilio`
- ✅ `WhatsApp.MetaCloud`
- ✅ Tests
- ✅ `dotnet test` — all green

---

## Phase 9 — Team Collaboration Adapters ✅

- ✅ `Notify.Slack`
- ✅ `Notify.Discord`
- ✅ `Notify.Teams`
- ✅ Tests
- ✅ `dotnet test` — all green

---

## Phase 10 — Social + Messaging Adapters ✅

- ✅ `Notify.Facebook`
- ✅ `Notify.Telegram`
- ✅ `Notify.Line`
- ✅ `Notify.Viber`
- ✅ Tests
- ✅ `dotnet test` — all green

---

## Phase 11 — InApp Channel ✅

- ✅ `Notify.InApp`
- ✅ Tests
- ✅ `dotnet test` — all green

---

## Phase 12 — Remaining Providers ✅

- ✅ `Email.Mailgun`, `Email.Resend`, `Email.Postmark`, `Email.AwsSes`, `Email.AzureCommEmail`
- ✅ `Sms.Vonage`, `Sms.Plivo`, `Sms.Sinch`, `Sms.MessageBird`, `Sms.AwsSns`, `Sms.AzureCommSms`
- ✅ `Push.OneSignal`, `Push.Expo`
- ✅ `WhatsApp.Vonage`
- ✅ `Notify.Mattermost`, `Notify.RocketChat`
- ✅ Tests
- ✅ `dotnet test` — all green

---

## Phase 13 — SDK Meta-Package + NuGet Publish (v0.2.0) ✅

- ✅ `RecurPixel.Notify.Sdk` meta-package
- ✅ NuGet metadata on all packages
- ✅ README.md with adapter test matrix
- ✅ `dotnet pack` all projects
- ✅ Published to NuGet.org as v0.2.0

---

---

# v0.3.0

> See ROADMAP.md for full feature details and upgrade instructions.

---

## Phase 14 — NotifyResult improvements + IHttpClientFactory fixes ✅

> Two workstreams, one phase.
>
> **14A — NotifyResult:** Smallest model change, biggest impact. BulkBatchId enables dashboard batch
> grouping. EventName and Metadata passthrough complete the OnDelivery hook story. No breaking changes.
>
> **14B — IHttpClientFactory:** All HTTP adapters registered as singletons but most call
> `CreateClient()` with no name — the typed registration is wasted and handler rotation doesn't work.
> Fix: named clients keyed by the adapter's DI key ("channel:provider"). Resolves DNS staleness,
> enables Phase 18 Polly hooks, and makes every adapter's HTTP config independently configurable.
> ApnsChannel uses `new HttpClient()` directly — real socket exhaustion risk, fix first.

### 14A — NotifyResult fields

- ✅ Add `EventName` (string?) to `NotifyResult`
- ✅ Add `BulkBatchId` (string?) to `NotifyResult`
- ✅ Add `Metadata` (Dictionary<string, object>?) to `NotifyResult`
- ✅ Orchestrator: generate `Guid` batch ID once per `BulkTriggerAsync` call
- ✅ Orchestrator: stamp `BulkBatchId` on every `NotifyResult` in that batch
- ✅ Orchestrator: populate `EventName` on every result from `TriggerAsync` / `BulkTriggerAsync`
- ✅ Orchestrator: pass `NotifyContext.Metadata` through to result
- ✅ Update unit tests — verify new fields are set correctly

### 14B — IHttpClientFactory named client fixes

> Pattern for every fix: replace `AddHttpClient<TChannel>()` or bare `AddHttpClient()` with
> `AddHttpClient("channel:provider", http => { /* BaseAddress, Timeout */ })` in
> ServiceCollectionExtensions. Replace `CreateClient()` with `CreateClient("channel:provider")`
> in the channel class. Never dispose the returned HttpClient (remove any `using var` wrappers).

**Highest priority — `new HttpClient()` direct instantiation (socket exhaustion risk):**
- ✅ `Push.Apns` — `ApnsChannel` calls `new HttpClient()` inside `CreateClient()`; pass an
  `IHttpClientFactory`-created client to `ApnsClient.CreateUsingJwt()` instead

**Named client fix — `AddHttpClient<T>()` typed registration wasted (calls `CreateClient()` unnamed):**
- ✅ `Email.Mailgun` → named client `"email:mailgun"`
- ✅ `Email.Postmark` → named client `"email:postmark"`
- ✅ `Email.Resend` → named client `"email:resend"`
- ✅ `Facebook` → named client `"facebook:default"`
- ✅ `Line` → named client `"line:default"`
- ✅ `Mattermost` → named client `"mattermost:default"`
- ✅ `Push.Expo` → named client `"push:expo"`
- ✅ `Push.OneSignal` → named client `"push:onesignal"`
- ✅ `RocketChat` → named client `"rocketchat:default"`
- ✅ `Sms.MessageBird` → named client `"sms:messagebird"`
- ✅ `Sms.Plivo` → named client `"sms:plivo"`
- ✅ `Sms.Sinch` → named client `"sms:sinch"`
- ✅ `Sms.Vonage` → named client `"sms:vonage"`
- ✅ `Telegram` → named client `"telegram:default"`
- ✅ `Viber` → named client `"viber:default"`
- ✅ `WhatsApp.Vonage` → named client `"whatsapp:vonage"`

**Named client fix — bare `AddHttpClient()`, no typed registration at all:**
- ✅ `Discord` → named client `"discord:default"` (also has `using var` dispose bug — remove it)
- ✅ `Slack` → named client `"slack:default"`
- ✅ `Teams` → named client `"teams:default"`

**Already correct — no changes needed:**
- `WhatsApp.MetaCloud` — already uses `AddHttpClient(nameof(...))` + `CreateClient(nameof(...))`
- `Email.SendGrid` — uses SendGrid SDK (SDK owns its own HttpClient)
- `Sms.Twilio` — uses Twilio SDK
- `Push.Fcm` — uses Firebase Admin SDK
- `WhatsApp.Twilio` — uses Twilio SDK

### Finish line

- ✅ `dotnet test` — all green (321 passed)

**Resume prompt:**
```
We are building RecurPixel.Notify — Phase 14. Two workstreams.

14A — NotifyResult (RecurPixel.Notify.Core/Models/NotifyResult.cs):
Add three nullable fields — no breaking changes, existing OnDelivery handlers need no modification.
- EventName (string?): populated by Orchestrator from the event name in TriggerAsync / BulkTriggerAsync
- BulkBatchId (string?): Guid generated once per BulkTriggerAsync call, stamped on every NotifyResult in that batch; null for single TriggerAsync
- Metadata (Dictionary<string, object>?): passed through from NotifyContext.Metadata

14B — IHttpClientFactory named client fixes across all HTTP-based adapters.
All channels are singletons (TryAddKeyedSingleton). Typed clients (AddHttpClient<T>) don't work
correctly with singletons — the named registration is wasted when CreateClient() is called
with no name. Fix: named clients using the adapter's existing DI key.

Pattern for each adapter:
  ServiceCollectionExtensions: replace AddHttpClient<TChannel>() or AddHttpClient()
    with: services.AddHttpClient("channel:provider", http => { http.Timeout = TimeSpan.FromSeconds(30); });
  Channel class: replace _httpClientFactory.CreateClient()
    with: _httpClientFactory.CreateClient("channel:provider");
  Remove any `using var` wrapper around the returned HttpClient (factory owns lifetime).

Adapters to fix and their named client keys:
  Email.Mailgun → "email:mailgun"
  Email.Postmark → "email:postmark"
  Email.Resend → "email:resend"
  Facebook → "facebook:default"
  Line → "line:default"
  Mattermost → "mattermost:default"
  Push.Expo → "push:expo"
  Push.OneSignal → "push:onesignal"
  RocketChat → "rocketchat:default"
  Sms.MessageBird → "sms:messagebird"
  Sms.Plivo → "sms:plivo"
  Sms.Sinch → "sms:sinch"
  Sms.Vonage → "sms:vonage"
  Telegram → "telegram:default"
  Viber → "viber:default"
  WhatsApp.Vonage → "whatsapp:vonage"
  Discord → "discord:default" (also remove `using var` around CreateClient result)
  Slack → "slack:default"
  Teams → "teams:default"

Special case — Push.Apns uses new HttpClient() directly inside a factory method:
  Add IHttpClientFactory to ApnsChannel constructor.
  Register named client "push:apns" in ApnsServiceCollectionExtensions.
  Pass _httpClientFactory.CreateClient("push:apns") to ApnsClient.CreateUsingJwt() instead of new HttpClient().

Do NOT change: WhatsApp.MetaCloud (already correct), Email.SendGrid, Sms.Twilio, Push.Fcm,
WhatsApp.Twilio (all use vendor SDKs that own their own HttpClient).
```

---

## Phase 15 — Dashboard data layer 🔲

> Phase 14 must be complete first — NotifyResult needs BulkBatchId before we design the log entity.
> Two packages: Dashboard (interface + entity) and Dashboard.EfCore (EF Core implementation).
> The Dashboard package auto-wires into OnDelivery during AddNotifyDashboard() — no user code change.

- 🔲 Create `src/RecurPixel.Notify.Dashboard` project
- 🔲 `NotificationLog.cs` entity (all NotifyResult fields + BulkBatchId, EventName, IsBulk)
- 🔲 `NotificationLogQuery.cs` — filter object (channel, status, date range, recipient, eventName, bulkBatchId)
- 🔲 `INotificationLogStore.cs` — AddAsync, QueryAsync, GetBatchAsync
- 🔲 `DashboardOptions.cs` — RoutePrefix, PageTitle, RequireRole, PolicyName, PageSize
- 🔲 Auto-wire: `AddNotifyDashboard()` registers an internal `OnDelivery` handler that writes to `INotificationLogStore`
- 🔲 Create `src/RecurPixel.Notify.Dashboard.EfCore` project
- 🔲 `NotifyDashboardDbContext.cs` — standalone context (Option A)
- 🔲 `NotificationLogEntityTypeConfiguration.cs` — `IEntityTypeConfiguration<NotificationLog>`
- 🔲 `ModelBuilderExtensions.cs` — `builder.AddNotifyDashboard()` for Option B (existing DbContext)
- 🔲 `EfCoreNotificationLogStore.cs` — implements `INotificationLogStore`
- 🔲 `AddNotifyDashboardEfCore()` extension on `IServiceCollection`
- 🔲 Add both packages to `RecurPixel.Notify.Sdk`
- 🔲 Write unit tests for store, query, entity configuration
- 🔲 `dotnet test` — all green

**Resume prompt:**
```
We are building RecurPixel.Notify — Phase 15.
Phase 14 is complete. NotifyResult now has EventName, BulkBatchId, and Metadata.
Goal: Dashboard data layer — two packages.

Package 1: RecurPixel.Notify.Dashboard
- NotificationLog entity (mirrors NotifyResult fields + IsBulk bool + BulkBatchId + EventName)
- INotificationLogStore: AddAsync, QueryAsync(NotificationLogQuery), GetBatchAsync(string bulkBatchId)
- DashboardOptions: RoutePrefix, PageTitle, RequireRole, PolicyName, PageSize
- AddNotifyDashboard() registers an internal OnDelivery handler that writes to INotificationLogStore
- Depends only on RecurPixel.Notify.Core — no EF Core reference

Package 2: RecurPixel.Notify.Dashboard.EfCore
- Implements INotificationLogStore using EF Core
- Ships IEntityTypeConfiguration<NotificationLog> applied via modelBuilder.AddNotifyDashboard()
- Also ships NotifyDashboardDbContext for users who want a standalone context
- AddNotifyDashboardEfCore(connectionString) extension registers EfCoreNotificationLogStore

No UI in this phase. Data layer only.
```

---

## Phase 16 — Dashboard UI + REST API + auth 🔲

> Phase 15 must be complete first — the UI is only as good as the query layer behind it.
> Pattern: Hangfire / Swagger. One middleware, one embedded HTML file, one JSON endpoint.
> No Razor Pages, no Blazor, no MVC dependency.

- 🔲 `NotifyDashboardMiddleware.cs` — intercepts requests to RoutePrefix
- 🔲 REST endpoint: `GET /notify-dashboard/api/logs` — accepts query params, returns paged JSON
- 🔲 REST endpoint: `GET /notify-dashboard/api/logs/batch/{batchId}` — returns all in a batch
- 🔲 REST endpoint: `GET /notify-dashboard/api/stats` — today's summary numbers
- 🔲 Authorization: check `HttpContext.User` against `RequireRole` / `PolicyName` before serving
- 🔲 Warn at startup (ILogger) if `RequireRole` and `PolicyName` are both null in non-Development environment
- 🔲 Embedded HTML dashboard — single file, served from middleware
  - 🔲 Summary row: total sent today, success rate, failure count, active channel count
  - 🔲 Filterable log table: time, channel badge, provider, recipient (truncated), subject (truncated), status badge, bulk icon
  - 🔲 Bulk row expand: click to show all recipients in the batch inline
  - 🔲 Failed row expand: click to show full error text
  - 🔲 Filters: channel dropdown, status (all/success/failed), date range, recipient search
- 🔲 `UseNotifyDashboard()` extension on `IApplicationBuilder`
- 🔲 Integration test: middleware serves page, JSON endpoints return correct shape
- 🔲 `dotnet test` — all green

**Resume prompt:**
```
We are building RecurPixel.Notify — Phase 16.
Phases 14-15 are complete. Dashboard data layer exists.
Goal: Dashboard middleware + embedded HTML UI + REST API + authorization.

Pattern: identical to Hangfire dashboard — one middleware class intercepts requests
to the configured RoutePrefix and serves an embedded HTML file. No Razor, no MVC.

Middleware serves:
- GET /notify-dashboard            → embedded index.html
- GET /notify-dashboard/api/logs   → JSON, query params: channel, status, from, to, recipient, page, pageSize
- GET /notify-dashboard/api/logs/batch/{batchId} → JSON array for one bulk batch
- GET /notify-dashboard/api/stats  → JSON: totalToday, successRate, failureCount, activeChannels

Authorization: before serving anything, check HttpContext.User against DashboardOptions.RequireRole
or DashboardOptions.PolicyName via IAuthorizationService. If both null and env is not Development,
log a startup warning via ILogger.

The embedded HTML calls the JSON endpoints. Plain JS, no framework. Chart.js or similar for
the summary row stats if bandwidth allows — otherwise plain numbers.
```

---

## Phase 17 — MSG91 adapters 🔲

> Can be built in parallel with phases 15-16. No dependency on dashboard.

- 🔲 Create `src/RecurPixel.Notify.Sms.Msg91` project
- 🔲 `Msg91SmsChannel : NotificationChannelBase`
- 🔲 `Msg91SmsOptions` — AuthKey, SenderId, Route
- 🔲 No native bulk API — base class loop handles it automatically
- 🔲 Create `src/RecurPixel.Notify.WhatsApp.Msg91` project
- 🔲 `Msg91WhatsAppChannel : NotificationChannelBase`
- 🔲 `Msg91WhatsAppOptions` — AuthKey, IntegratedNumber, Namespace
- 🔲 Add both to `RecurPixel.Notify.Sdk`
- 🔲 Update adapter status table in README
- 🔲 Unit tests for both
- 🔲 `dotnet test` — all green

**Resume prompt:**
```
We are building RecurPixel.Notify — Phase 17.
Goal: MSG91 adapters for SMS and WhatsApp.

Package 1: RecurPixel.Notify.Sms.Msg91
- Msg91SmsChannel extends NotificationChannelBase
- Msg91SmsOptions: AuthKey, SenderId, Route (default "4")
- MSG91 SMS API: POST https://api.msg91.com/api/v5/flow/
- No native bulk — base class loop handles bulk automatically, no SendBulkAsync override

Package 2: RecurPixel.Notify.WhatsApp.Msg91
- Msg91WhatsAppChannel extends NotificationChannelBase
- Msg91WhatsAppOptions: AuthKey, IntegratedNumber, Namespace
- MSG91 WhatsApp API for template messages
- No native bulk

Standard adapter rules apply:
- Catch all exceptions, return NotifyResult { Success = false, Error = ex.Message }
- ILogger<T> for attempt/success/failure logging
- No template logic, no content validation, no DB access
```

---

---

# v0.4.0

> Planned. Design docs to be written before build starts.

---

## Phase 18 — Polly hooks (IHttpClientBuilder per adapter) 🔲

> Expose IHttpClientBuilder per adapter so users can attach Polly policies.
> The library does not own resilience configuration — users do.

**Resume prompt:**
```
We are building RecurPixel.Notify — Phase 18.
Goal: expose IHttpClientBuilder hooks per adapter so users can attach Polly policies.
Pattern: AddRecurPixelNotify() returns a builder that exposes ConfigureEmailHttpClient(),
ConfigureSmsHttpClient() etc. Each wires into the named HttpClient for that adapter.
No Polly dependency inside the library. User brings Polly.
```

---

## Phase 19 — OpenTelemetry integration 🔲

> Add Activity tracing around every send attempt.
> One optional extension: AddRecurPixelNotifyInstrumentation().
> No mandatory dependency on OpenTelemetry packages.

**Resume prompt:**
```
We are building RecurPixel.Notify — Phase 19.
Goal: optional OpenTelemetry instrumentation package.
Package: RecurPixel.Notify.OpenTelemetry
Extension: .AddRecurPixelNotifyInstrumentation() on TracerProviderBuilder.
Emits one Activity per send attempt.
Tags: notify.channel, notify.provider, notify.event_name, notify.success,
      notify.recipient_hash (SHA256 first 8 chars — not raw PII),
      notify.used_fallback, notify.bulk_batch_id.
No Activity emitted if OpenTelemetry is not registered — guard with ActivitySource.HasListeners().
```

---

## Phase 20 — Circuit breaker 🔲

> Per-provider circuit state inside the Orchestrator.
> Design doc required before implementation — write it as a supplement (like Bulk and MultiProvider docs).

- 🔲 Write `RecurPixel.Notify-CircuitBreaker.md` design document first
- 🔲 Implement after design is approved

---

## Phase 21 — Additional adapters 🔲

Candidates:
- `Sms.Kaleyra`
- `WhatsApp.Gupshup`
- `WhatsApp.AiSensy`
- Community-submitted adapters that pass integration tests

---

## Phase 22 — Dashboard v2 🔲

> Builds on Dashboard v1 (Phase 16). Adds write operations and richer views.

- 🔲 Retry action — resend a failed notification from the dashboard
- 🔲 Batch detail page — dedicated URL for a single bulk batch
- 🔲 Provider health row — per-provider success rate in last 24h in summary row
- 🔲 CSV export — download filtered log results

---

---

## Adapter status

| Package | Provider | Channel | Unit tested | Integration tested | Community approved |
|---|---|---|---|---|---|
| `Email.SendGrid` | Twilio SendGrid | Email | ✅ | ✅ | 🔲 |
| `Email.Smtp` | Any SMTP server | Email | ✅ | ✅ | 🔲 |
| `Email.Mailgun` | Mailgun | Email | ✅ | 🔲 | 🔲 |
| `Email.Resend` | Resend | Email | ✅ | 🔲 | 🔲 |
| `Email.Postmark` | Postmark | Email | ✅ | 🔲 | 🔲 |
| `Email.AwsSes` | AWS SES | Email | ✅ | 🔲 | 🔲 |
| `Email.AzureCommEmail` | Azure Communication Services | Email | ✅ | 🔲 | 🔲 |
| `Sms.Twilio` | Twilio | SMS | ✅ | ✅ | 🔲 |
| `Sms.Vonage` | Vonage (Nexmo) | SMS | ✅ | 🔲 | 🔲 |
| `Sms.Plivo` | Plivo | SMS | ✅ | 🔲 | 🔲 |
| `Sms.Sinch` | Sinch | SMS | ✅ | 🔲 | 🔲 |
| `Sms.MessageBird` | MessageBird | SMS | ✅ | 🔲 | 🔲 |
| `Sms.AwsSns` | AWS SNS | SMS | ✅ | 🔲 | 🔲 |
| `Sms.AzureCommSms` | Azure Communication Services | SMS | ✅ | 🔲 | 🔲 |
| `Sms.Msg91` | MSG91 | SMS | 🔲 | 🔲 | 🔲 |
| `Push.Fcm` | Firebase Cloud Messaging | Push | ✅ | 🔲 | 🔲 |
| `Push.Apns` | Apple Push Notification Service | Push | ✅ | 🔲 | 🔲 |
| `Push.OneSignal` | OneSignal | Push | ✅ | 🔲 | 🔲 |
| `Push.Expo` | Expo Push | Push | ✅ | 🔲 | 🔲 |
| `WhatsApp.Twilio` | Twilio WhatsApp | WhatsApp | ✅ | ✅ | 🔲 |
| `WhatsApp.MetaCloud` | Meta Cloud API | WhatsApp | ✅ | 🔲 | 🔲 |
| `WhatsApp.Vonage` | Vonage WhatsApp | WhatsApp | ✅ | 🔲 | 🔲 |
| `WhatsApp.Msg91` | MSG91 WhatsApp | WhatsApp | 🔲 | 🔲 | 🔲 |
| `Slack` | Slack Webhooks / Bot API | Team Chat | ✅ | ✅ | 🔲 |
| `Discord` | Discord Webhooks | Team Chat | ✅ | ✅ | 🔲 |
| `Teams` | Microsoft Teams Webhooks | Team Chat | ✅ | 🔲 | 🔲 |
| `Mattermost` | Mattermost Webhooks | Team Chat | ✅ | 🔲 | 🔲 |
| `RocketChat` | Rocket.Chat Webhooks | Team Chat | ✅ | 🔲 | 🔲 |
| `Facebook` | Meta Messenger API | Social | ✅ | 🔲 | 🔲 |
| `Telegram` | Telegram Bot API | Social | ✅ | ✅ | 🔲 |
| `Line` | LINE Messaging API | Social | ✅ | 🔲 | 🔲 |
| `Viber` | Viber Business Messages | Social | ✅ | 🔲 | 🔲 |
| `InApp` | Hook-based (user-defined storage) | In-App | ✅ | ✅ | 🔲 |

> **Unit tested** — complete test coverage of the adapter contract using mocked HTTP/SDK.
> **Integration tested** — verified against a live provider API with a real account.
> **Community approved** — integration-tested and reviewed by a community member outside RecurPixel. PRs welcome.

---

## Native bulk support reference

| Channel | Provider | Override SendBulkAsync? | Limit |
|---|---|---|---|
| Email | SendGrid | ✅ Yes | 1000/call |
| Email | AwsSes | ✅ Yes | batch API |
| Email | Postmark | ✅ Yes | batch endpoint |
| Email | Mailgun | ✅ Yes | recipient variables |
| Email | Resend | ❌ No | no batch API |
| Email | SMTP | ❌ No | single send protocol |
| SMS | Twilio | ❌ No | no batch API |
| SMS | Vonage | ✅ Yes | bulk SMS API |
| SMS | AwsSns | ✅ Yes | topic publish |
| SMS | Sinch | ✅ Yes | batch SMS API |
| SMS | MSG91 | ❌ No | no batch API |
| Push | FCM | ✅ Yes | 500 tokens/call |
| Push | APNs | ❌ No | one per call |
| Push | OneSignal | ✅ Yes | bulk notifications API |
| Push | Expo | ✅ Yes | push tickets batch |
| WhatsApp | Any | ❌ No | Meta policy restricts bulk |
| Slack | — | ❌ No | one per webhook |
| Discord | — | ❌ No | one per webhook |
| Teams | — | ❌ No | one per webhook |
| Telegram | — | ❌ No | no bulk DM |
| Facebook | — | ❌ No | per-user Messenger API |

---

*RecurPixel.Notify — Build Tracker. Updated: April 2026.*
