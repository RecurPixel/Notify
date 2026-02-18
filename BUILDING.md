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

## Phase 2 — Test Project + First Two Adapters 🔲

> Goal: prove the adapter pattern works end-to-end before building more.
> Deliverables: xUnit test project, `Email.SendGrid` adapter, `Email.Smtp` adapter.

- ✅ Create `tests/RecurPixel.Notify.Tests` xUnit project
- ✅ Add test project to solution
- ✅ Reference Core from test project
- ✅ Write Core contract tests (channel base, bulk result, options validator)
- ✅ Create `src/RecurPixel.Notify.Email.SendGrid` project
- ✅ Implement `SendGridChannel : NotificationChannelBase`
- ✅ Implement `SendGridChannel.SendBulkAsync` (native batch override)
- ✅ Write SendGrid adapter unit tests
- ✅ Create `src/RecurPixel.Notify.Email.Smtp` project
- ✅ Implement `SmtpChannel : NotificationChannelBase`
- ✅ Write Smtp adapter unit tests
- ✅ `dotnet test` — all green

**Resume prompt for this phase:**
```
We are building RecurPixel.Notify — a modular NuGet notification library for ASP.NET Core.
Phase 1 (Core) is complete. We are now on Phase 2.
Goal: xUnit test project + Email.SendGrid adapter + Email.Smtp adapter.
The Core library is at src/RecurPixel.Notify.Core.
Key contracts: INotificationChannel, NotificationChannelBase, NotificationPayload, NotifyResult, BulkNotifyResult.
All adapters extend NotificationChannelBase (never implement INotificationChannel directly).
Please start with the test project setup.
```

---

## Phase 3 — SMS Adapter `Sms.Twilio` 🔲

> Goal: validate the cross-channel pattern. Prove the same adapter structure works for SMS.

- ✅ Create `src/RecurPixel.Notify.Sms.Twilio` project
- ✅ Implement `TwilioSmsChannel : NotificationChannelBase`
- ✅ Write Twilio SMS adapter unit tests
- ✅ `dotnet test` — all green

**Resume prompt:**
```
We are building RecurPixel.Notify — Phase 3.
Phases 1 (Core) and 2 (SendGrid + Smtp adapters + test project) are complete.
Goal: implement Sms.Twilio adapter.
Adapter must extend NotificationChannelBase from RecurPixel.Notify.Core.
Twilio has no native SMS bulk API — base class loop handles bulk automatically, no override needed.
```

---

## Phase 4 — Orchestrator `RecurPixel.Notify.Orchestrator` 🔲

> The event system, TriggerAsync, BulkTriggerAsync, conditions, parallel dispatch.
> This is the largest single phase — may need two chat sessions.

- ✅ Create `src/RecurPixel.Notify.Orchestrator` project
- ✅ `EventDefinition` — channel list, conditions, retry, fallback config per event
- ✅ `EventRegistry` — stores and retrieves event definitions
- ✅ `INotifyService` interface (with Trigger, BulkTrigger, direct channel properties)
- ✅ `NotifyService` implementation
- ✅ `TriggerAsync` — single user orchestrated send
- ✅ `BulkTriggerAsync` — multi-user orchestrated send
- ✅ Parallel dispatch via `Task.WhenAll`
- ✅ Condition evaluation against `NotifyContext`
- ✅ Multi-provider resolution (default → named → fallback)
- ✅ Keyed DI registration per channel+provider (`"email:sendgrid"`, `"sms:twilio"`)
- ✅ Delivery hook wiring (`OnDelivery` called per result)
- ✅ Write orchestrator unit tests
- ✅ `dotnet test` — all green

**Resume prompt:**
```
We are building RecurPixel.Notify — Phase 4.
Phases 1–3 are complete (Core, SendGrid, Smtp, Twilio SMS adapters, test project).
Goal: implement the Orchestrator — event registry, TriggerAsync, BulkTriggerAsync, conditions, parallel dispatch, multi-provider resolution.
Adapters are registered in DI keyed by "{channel}:{provider}" e.g. "email:sendgrid".
The Orchestrator resolves adapters via IServiceProvider.GetRequiredKeyedService.
Multi-provider resolution order: Metadata["provider"] named routing → default Provider → Fallback.
OnDelivery hook is called per individual NotifyResult, never per BulkNotifyResult.
```

---

## Phase 5 — Retry + Fallback ✅

> Retry with exponential backoff. Cross-channel fallback chains. Both inside the Orchestrator.

- ✅ Retry policy engine (MaxAttempts, DelayMs, ExponentialBackoff)
- ✅ Per-event retry override
- ✅ Cross-channel fallback chain execution
- ✅ Per-event fallback override
- ✅ Write retry + fallback unit tests
- ✅ `dotnet test` — all green

**Resume prompt:**
```
We are building RecurPixel.Notify — Phase 5.
Phases 1–4 are complete (Core, adapters, test project, Orchestrator with TriggerAsync/BulkTriggerAsync/conditions/multi-provider).
Goal: add retry with exponential backoff and cross-channel fallback chains inside the Orchestrator.
RetryOptions: MaxAttempts, DelayMs, ExponentialBackoff. Can be global or per-event.
FallbackOptions: Chain array of channel names. Tried in order if the current channel fails after retries.
```

---

## Phase 6 — Delivery Hook + ILogger 🔲

- ✅ `OnDelivery` callback wiring into Orchestrator dispatch
- ✅ `ILogger<T>` structured logging in all adapters (attempt, success, failure)
- ✅ `ILogger<T>` structured logging in Orchestrator
- ✅ Write hook + logging tests
- ✅ `dotnet test` — all green

**Resume prompt:**
```
We are building RecurPixel.Notify — Phase 6.
Phases 1–5 are complete (Core, adapters, Orchestrator, retry, fallback).
Goal: wire OnDelivery callback and add ILogger<T> structured logging.
OnDelivery is defined on NotifyOptions as Func<NotifyResult, Task>.
It is called for every individual NotifyResult — once per send attempt.
ILogger<T> is injected — no custom logging abstraction.
```

---

## Phase 7 — Push Adapters `Push.Fcm` + `Push.Apns` 🔲

- ✅ Create `src/RecurPixel.Notify.Push.Fcm` project
- ✅ Implement `FcmChannel` with `SendBulkAsync` override (500 tokens/multicast call)
- ✅ Create `src/RecurPixel.Notify.Push.Apns` project
- ✅ Implement `ApnsChannel` (no native bulk — base loop handles it)
- ✅ Write tests for both
- ✅ `dotnet test` — all green

**Resume prompt:**
```
We are building RecurPixel.Notify — Phase 7.
Phases 1–6 are complete.
Goal: Push.Fcm and Push.Apns adapters.
FCM supports multicast — override SendBulkAsync, chunk payloads into 500 per call.
APNs has no bulk API — extend NotificationChannelBase, implement SendAsync only.
Set UsedNativeBatch = true for FCM, false for APNs (handled by base class).
```

---

## Phase 8 — WhatsApp Adapters 🔲

- ✅ `WhatsApp.Twilio`
- ✅ `WhatsApp.MetaCloud`
- ✅ Both use base class loop (Meta policy restricts bulk WhatsApp)
- ✅ Tests + `dotnet test` green

**Resume prompt:**
```
We are building RecurPixel.Notify — Phase 8.
Goal: WhatsApp.Twilio and WhatsApp.MetaCloud adapters.
Neither provider supports bulk WhatsApp — extend NotificationChannelBase, implement SendAsync only.
Meta Cloud API requires phone number ID and access token from WhatsAppOptions.MetaCloud.
```

---

## Phase 9 — Team Collaboration Adapters 🔲

> Simplest adapters — all webhook-based, no auth complexity.

- ✅ `Notify.Slack` (webhook + optional Bot API)
- ✅ `Notify.Discord` (webhook)
- ✅ `Notify.Teams` (webhook)
- ✅ Tests + `dotnet test` green

**Resume prompt:**
```
We are building RecurPixel.Notify — Phase 9.
Goal: Slack, Discord, and Teams webhook adapters.
All three are simple HTTP POST webhook senders.
All extend NotificationChannelBase, implement SendAsync only, no bulk override needed.
Slack: post to WebhookUrl. Body = message text. Subject = optional header.
Discord: post to WebhookUrl. JSON body with "content" field.
Teams: post to WebhookUrl. Adaptive Card or simple text body.
```

---

## Phase 10 — Social + Messaging Adapters 🔲

- ✅ `Notify.Facebook` (Messenger API)
- ✅ `Notify.Telegram` (Bot API)
- ✅ `Notify.Line`
- ✅ `Notify.Viber`
- ✅ Tests + `dotnet test` green

---

## Phase 11 — InApp Channel 🔲

- ✅ `Notify.InApp` — hook-based, user defines storage
- ✅ Tests + `dotnet test` green

---

## Phase 12 — Remaining Providers 🔲

- 🔲 `Email.Mailgun`
- 🔲 `Email.Resend`
- 🔲 `Email.Postmark`
- 🔲 `Email.AwsSes`
- 🔲 `Sms.Vonage` (native bulk SMS API — override SendBulkAsync)
- 🔲 `Sms.Plivo`
- 🔲 `Sms.Sinch` (native bulk SMS API — override SendBulkAsync)
- 🔲 `Sms.MessageBird`
- 🔲 `Sms.AwsSns`
- 🔲 `Push.OneSignal` (native bulk — override SendBulkAsync)
- 🔲 `Push.Expo` (native bulk — override SendBulkAsync)
- 🔲 `WhatsApp.Vonage`
- 🔲 Tests for all + `dotnet test` green

---

## Phase 13 — SDK Meta-Package + NuGet Publish 🔲

- 🔲 Create `src/RecurPixel.Notify.Sdk` meta-package project
- 🔲 Set all `.csproj` NuGet metadata (author, description, tags, license, icon)
- 🔲 Set package versions
- 🔲 Write README.md
- 🔲 `dotnet pack` all projects
- 🔲 Test install from local NuGet feed
- 🔲 Publish to NuGet.org

---

## Native Bulk Support Quick Reference

> When building adapters — check here first before deciding whether to override SendBulkAsync.

| Channel  | Provider  | Override SendBulkAsync? | Limit                      |
| -------- | --------- | ----------------------- | -------------------------- |
| Email    | SendGrid  | ✅ Yes                   | 1000/call                  |
| Email    | AwsSes    | ✅ Yes                   | batch API                  |
| Email    | Postmark  | ✅ Yes                   | batch endpoint             |
| Email    | Mailgun   | ✅ Yes                   | recipient variables        |
| Email    | Resend    | ❌ No                    | no batch API               |
| Email    | SMTP      | ❌ No                    | single send protocol       |
| SMS      | Twilio    | ❌ No                    | no batch API               |
| SMS      | Vonage    | ✅ Yes                   | bulk SMS API               |
| SMS      | AwsSns    | ✅ Yes                   | topic publish              |
| SMS      | Sinch     | ✅ Yes                   | batch SMS API              |
| Push     | FCM       | ✅ Yes                   | 500 tokens/call            |
| Push     | APNs      | ❌ No                    | one per call               |
| Push     | OneSignal | ✅ Yes                   | bulk notifications API     |
| Push     | Expo      | ✅ Yes                   | push tickets batch         |
| WhatsApp | Any       | ❌ No                    | Meta policy restricts bulk |
| Slack    | —         | ❌ No                    | one per webhook            |
| Discord  | —         | ❌ No                    | one per webhook            |
| Teams    | —         | ❌ No                    | one per webhook            |
| Telegram | —         | ❌ No                    | no bulk DM                 |
| Facebook | —         | ❌ No                    | per-user Messenger API     |

---

*RecurPixel.Notify — Build Tracker. Updated: February 2026.*
