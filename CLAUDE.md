# RecurPixel.Notify — Claude Code context

## What this project is

RecurPixel.Notify is a modular, DI-native NuGet library for ASP.NET Core that delivers
notifications across Email, SMS, Push, WhatsApp, Slack, Discord, Teams, Telegram, and more.
Pure library — no SaaS, no external platform, no queue, no template engine.

## Read these files before starting any phase

- `BUILDING.md` — current build status and phase checklist. Find the first phase marked 🔲.
- `ROADMAP.md` — version scope (v0.3.0, v0.4.0) and what is explicitly out of scope.
- `.claude/docs/core-philosophy.md` — contracts, coding rules, adapter prompt template.
- `.claude/docs/bulk-design.md` — bulk/batch design rules. Read for any phase touching SendBulkAsync.
- `.claude/docs/multi-provider-design.md` — multi-provider and fallback rules. Read for Orchestrator work.

## Project structure

```
src/
  RecurPixel.Notify.Core/           ← interfaces, models, base class — no dependencies
  RecurPixel.Notify.Orchestrator/   ← event system, TriggerAsync, conditions, dispatch
  RecurPixel.Notify.Email.SendGrid/ ← adapter pattern — all adapters follow this
  RecurPixel.Notify.Email.Smtp/
  RecurPixel.Notify.Sms.Twilio/
  RecurPixel.Notify.Dashboard/      ← v0.3.0 — not yet built
  RecurPixel.Notify.Dashboard.EfCore/
  RecurPixel.Notify.Sdk/            ← meta-package, pulls everything
tests/
  RecurPixel.Notify.Tests/
```

## Commands

```bash
dotnet build
dotnet test tests/RecurPixel.Notify.Tests/
dotnet pack src/RecurPixel.Notify.Core/
```

## Non-negotiable rules — always apply

- Every adapter extends `NotificationChannelBase`, never `INotificationChannel` directly
- No adapter references another adapter — ever
- All exceptions caught inside adapters, returned as `NotifyResult { Success = false, Error = ex.Message }`
- No template logic, no content validation, no DB access inside any adapter or the core library
- `OnDelivery` hook called per individual `NotifyResult` — never per `BulkNotifyResult`
- No EF Core, no Dapper, no ORM inside any package except `Dashboard.EfCore`
- Every public class, interface, and method must have XML doc comments
- `sealed` on classes where inheritance is not intended
- Internal implementation classes are `internal`, not `public`
- Language: C# 12 / .NET 8+
- `netstandard2.1` for Core; `net8.0` for adapters that require it

## What this library will NEVER build

- Template engine — user owns subject, body, HTML. We deliver it, we don't build it.
- Queue / background dispatcher — user calls TriggerAsync from their own Hangfire/Quartz job
- Notification log storage — OnDelivery hook exists; user writes to their own DB
- Load balancing or A/B testing across providers
- Scheduled send (needs separate design doc before any code)

## How to start a phase

Find the first phase marked 🔲 in BUILDING.md.
Read the Resume prompt at the bottom of that phase section.
That prompt is your starting context — use it directly.

## When a phase is complete

- All checkboxes in that phase ticked
- `dotnet test` green
- Commit with message: `feat: Phase N — [phase name]`
- Update BUILDING.md checkboxes (mark ✅)
