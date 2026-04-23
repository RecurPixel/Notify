---
layout: default
title: Migration Guide
nav_order: 9
---

# Migration Guide

---

## v0.2.0 → v0.3.0

**No breaking changes.** All v0.2.0 code compiles and runs without modification.

v0.3.0 adds four new packages and three new fields on `NotifyResult`. Opt in to what you want.

### What's new (opt-in)

**New packages:**
- `RecurPixel.Notify.Dashboard` + `RecurPixel.Notify.Dashboard.EfCore` — delivery log UI and REST API. See [Dashboard](dashboard) to set it up.
- `RecurPixel.Notify.Sms.Msg91` — MSG91 SMS adapter
- `RecurPixel.Notify.WhatsApp.Msg91` — MSG91 WhatsApp Business adapter

**New `NotifyResult` fields:**

| Field          | Type                        | Description                                           |
| -------------- | --------------------------- | ----------------------------------------------------- |
| `EventName`    | `string?`                   | The event name that produced this result              |
| `BulkBatchId`  | `string?`                   | Shared correlation ID across all results in a bulk send |
| `Subject`      | `string?`                   | Notification subject, populated from the payload      |

These are nullable — existing `OnDelivery` hooks that don't reference them continue to work with no changes.

### Upgrade checklist

- [ ] Update `RecurPixel.Notify.Sdk` (or individual packages) to `0.3.0`
- [ ] Optionally install `RecurPixel.Notify.Dashboard` and `RecurPixel.Notify.Dashboard.EfCore` — see [Dashboard](dashboard)
- [ ] Optionally install `RecurPixel.Notify.Sms.Msg91` or `RecurPixel.Notify.WhatsApp.Msg91`
- [ ] Optionally add `EventName`, `BulkBatchId`, `Subject` to your `OnDelivery` log table

---

## v0.1.0-beta.1 → v0.2.0

v0.2.0 introduced breaking changes in package structure and namespace layout.

### Breaking Changes

**1. Package structure**

`RecurPixel.Notify.Core` and `RecurPixel.Notify.Orchestrator` are merged into `RecurPixel.Notify`:

```bash
dotnet remove package RecurPixel.Notify.Core
dotnet remove package RecurPixel.Notify.Orchestrator
dotnet add package RecurPixel.Notify
```

**2. Namespace reorganization**

Update your `using` statements:

| Old Namespace                             | New Namespace                                                                                      |
| ----------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `RecurPixel.Notify.Core.Models`           | `RecurPixel.Notify`                                                                                |
| `RecurPixel.Notify.Core.Channels`         | `RecurPixel.Notify.Channels`                                                                       |
| `RecurPixel.Notify.Core.Options`          | `RecurPixel.Notify` (core options) or `RecurPixel.Notify.Configuration` (channel/provider options) |
| `RecurPixel.Notify.Orchestrator.Services` | `RecurPixel.Notify`                                                                                |
| `RecurPixel.Notify.[Channel].[Provider]`  | `RecurPixel.Notify` (for ServiceCollectionExtensions)                                              |

**3. Return types**

`TriggerAsync` now returns strongly-typed `TriggerResult` (was `dynamic`). `BulkTriggerAsync` returns `BulkTriggerResult`.

```csharp
// Before (v0.1.0-beta.1)
dynamic result = await notify.TriggerAsync(...);
if (result.Success) { ... }

// After (v0.2.0+)
TriggerResult result = await notify.TriggerAsync(...);
if (result.AllSucceeded) { ... }
foreach (var failure in result.Failures) { ... }
```

**4. InApp handler setup**

`notifyOptions.InApp` and `notifyOptions.OnDelivery` properties are replaced with explicit calls:

```csharp
// Before (v0.1.0-beta.1)
notifyOptions.InApp = new() { /* ... */ };
notifyOptions.OnDelivery = async result => { /* ... */ };

// After (v0.2.0+)
// ① InApp handler — separate call before AddRecurPixelNotify
builder.Services.AddInAppChannel(opts =>
    opts.UseHandler<IApplicationDbContext>(async (notification, db) => { /* ... */ }));

// ② Main registration
builder.Services.AddRecurPixelNotify(
    notifyOptions => { /* ... */ },
    orchestratorOptions =>
    {
        // ③ Delivery hook — inside AddRecurPixelNotify
        orchestratorOptions.OnDelivery<IApplicationDbContext>(async (result, db) => { /* ... */ });
    });
```

**Key distinction:**
- **`UseHandler`** — where you implement the send (e.g. write InApp notifications to DB)
- **`OnDelivery`** — audit hook that fires after every send, for logging and metrics

### Code Update Checklist

- [ ] Remove `RecurPixel.Notify.Core` and `RecurPixel.Notify.Orchestrator` packages
- [ ] Add `RecurPixel.Notify` package
- [ ] Update `using RecurPixel.Notify.Core.*` → `using RecurPixel.Notify`
- [ ] Update `using RecurPixel.Notify.Core.Options.*` → `using RecurPixel.Notify.Configuration`
- [ ] Update `using RecurPixel.Notify.Core.Channels` → `using RecurPixel.Notify.Channels`
- [ ] Update `using RecurPixel.Notify.Orchestrator.Services` → `using RecurPixel.Notify`
- [ ] Move `notifyOptions.InApp` logic into `UseHandler<T>` call
- [ ] Move `notifyOptions.OnDelivery` logic into `OnDelivery<T>` call
- [ ] Update code that inspects `TriggerResult` (now strongly typed, not dynamic)
