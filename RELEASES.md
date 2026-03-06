# Releases

Quick reference for each released version. For comprehensive change details, see [CHANGELOG.md](changelogs/CHANGELOG.md).

---

## v0.2.0-beta.1 (March 2026)

**Key Changes:**
- **Namespace Reorganization** — cleaner public API: models and services in `RecurPixel.Notify`, channels in `RecurPixel.Notify.Channels`, options in `RecurPixel.Notify.Configuration`
- **New Meta-Package** — `RecurPixel.Notify` now bundles Core + Orchestrator (replaces separate packages)
- **Typed Return Values** — `TriggerAsync` returns strongly-typed `TriggerResult` (not dynamic) with per-channel inspection
- **Bulk Send** — `BulkTriggerAsync` returns `BulkTriggerResult` for multi-user event dispatch
- **InApp Handler Pattern** — explicit `UseHandler<T>` replaces `notifyOptions.InApp` callback
- **OnDelivery Hook** — now separate from send implementation, purely for audit logging
- **New Adapters** — added Azure Communication Services (Email + SMS), Mattermost, RocketChat

**Breaking Changes:**
- Package structure: remove `RecurPixel.Notify.Core` and `RecurPixel.Notify.Orchestrator`, add `RecurPixel.Notify`
- Namespace updates required in all imports (see [Migration Guide](https://recurpixel.github.io/Notify#migration-from-v010-beta1))
- `TriggerResult` / `BulkTriggerResult` are now strongly typed
- InApp requires `AddInAppChannel(opts => opts.UseHandler(...))` before `AddRecurPixelNotify`

**Migration Path:**
→ See [Migration Guide](https://recurpixel.github.io/Notify#migration-from-v010-beta1) for upgrade checklist and code examples.

**Package Count:** 35 (Core + Orchestrator + RecurPixel.Notify meta-package + 31 adapters + SDK meta-package)

**Tests:** 314 passing

---

## v0.1.0-beta.1 (February 2026)

Initial beta release.

**Features:**
- 30 provider adapters across 12 channels (Email, SMS, Push, WhatsApp, Chat, Social, In-App)
- Event-driven orchestration with retry/fallback
- Parallel dispatch and bulk send
- Delivery hooks via `OnDelivery` callback
- Direct channel access for time-critical flows
- Three usage tiers (single-channel, selective providers, full SDK)

**Package Count:** 32 (Core + Orchestrator + 30 adapters)

---

## Support & Feedback

- **Issues:** [GitHub Issues](https://github.com/RecurPixel/Notify/issues)
- **Discussions:** [GitHub Discussions](https://github.com/RecurPixel/Notify/discussions)
- **Docs:** [Official Documentation](https://recurpixel.github.io/Notify)
