# RecurPixel.Notify — API & Auto-Registration Cleanup

> This document tracks five issues discovered during real-world usage of beta.2.
> Each issue has its root cause, the fix, and a checkbox per file changed.

---

## Issue 1 — Two `AddRecurPixelNotify` methods (API confusion)

**Root cause:** Both Core and Orchestrator expose a method named `AddRecurPixelNotify`. Core's version only registers the options POCO; Orchestrator's version does everything (options + adapter discovery + DI wiring). Users calling the Core version by mistake end up with no adapters registered and a confusing runtime error.

**Fix:** Rename Core's overloads to `AddNotifyOptions`. Orchestrator's `AddRecurPixelNotify(configureOptions, configureOrchestrator)` stays as the primary user-facing entry point.

- [x] `src/RecurPixel.Notify.Core/Extensions/ServiceCollectionExtensions.cs` — rename both overloads
- [x] `src/RecurPixel.Notify.Orchestrator/Extensions/ServiceCollectionExtensions.cs` — update internal call from `AddRecurPixelNotify` → `AddNotifyOptions`

---

## Issue 2 — `InAppOptions.OnDeliver` vs `OrchestratorOptions.OnDelivery` (naming confusion)

**Root cause:** Both names sound like "subscribe to a delivery event" but they mean completely different things:
- `InAppOptions.OnDeliver(handler)` — provides the *implementation* of the send (storage, SignalR, etc.). This IS the delivery.
- `OrchestratorOptions.OnDelivery(handler)` — audit callback called *after* every channel send attempt. This OBSERVES delivery.

**Fix:** Rename `InAppOptions.OnDeliver` → `UseHandler` to make it clear you're providing the implementation, not subscribing to results.

- [x] `src/RecurPixel.Notify.Core/Options/MessagingProviderOptions.cs` — rename both `OnDeliver` overloads to `UseHandler`
- [x] `tests/RecurPixel.Notify.Tests/InApp/InAppChannelTests.cs` — update all call sites
- [x] `tests/RecurPixel.Notify.IntegrationTests/InApp/InAppIntegrationTests.cs` — update call site

---

## Issue 3 — Dead code in `NotifyOptions` (misleading API surface)

**Root cause:** `NotifyOptions` has two properties that can never be meaningfully set:
- `OnDelivery: Func<NotifyResult, Task>?` — the real audit hook lives in `OrchestratorOptions._deliveryHandlers`; this property is never read by ChannelDispatcher or NotifyService.
- `InApp: InAppOptions?` — `InAppOptions.DeliverHandler` is a code delegate (not JSON-serializable); IConfiguration binding can never set it; the scanner registers InApp unconditionally without reading this property.

**Fix:** Remove both dead properties.

- [x] `src/RecurPixel.Notify.Core/Options/NotifyOptions.cs` — remove `OnDelivery` and `InApp` properties

---

## Issue 4 — Auto-registration fails silently (assembly not loaded)

**Root cause:** `DiscoverAdapters()` uses `AppDomain.CurrentDomain.GetAssemblies()`. In .NET, assemblies are loaded on demand — adapter DLLs in the output directory are not loaded until code from them is first executed. At startup (when `RegisterAdapters` runs during DI setup), no adapter code has been touched yet, so the scan finds nothing.

**Fix:** Add `EnsureAdapterAssembliesLoaded()` called at the top of `RegisterAdapters()`. It scans `AppDomain.CurrentDomain.BaseDirectory` for `RecurPixel.Notify.*.dll` files and loads any that aren't already in the AppDomain.

- [x] `src/RecurPixel.Notify.Orchestrator/Extensions/ServiceCollectionExtensions.cs` — add `EnsureAdapterAssembliesLoaded()` + `using System.IO; using System.Reflection;`

---

## Issue 5 — "No adapter registered for key 'email:default'" (consequence of 1 & 4)

**Root cause:** Either (a) auto-registration never ran (wrong setup method called — Issue 1), or (b) the scanner ran but found nothing because assemblies weren't loaded (Issue 4), or (c) `Email.Provider` was set to the literal string `"default"` which is not a valid provider key. Issues 1 and 4 are the primary root causes; Issue 5 is their symptom.

**Fix:** Improve the error message in `ResolveAdapter` to include actionable next steps.

- [x] `src/RecurPixel.Notify.Orchestrator/Dispatch/ChannelDispatcher.cs` — improve error message in `ResolveAdapter`

---

## Status

| Phase | Issue | Status |
|-------|-------|--------|
| A1 | Core rename `AddRecurPixelNotify` → `AddNotifyOptions` | [x] |
| A2 | Remove `NotifyOptions.OnDelivery` + `NotifyOptions.InApp` | [x] |
| A3 | Update Orchestrator's combined overload internal call | [x] |
| B1 | Rename `InAppOptions.OnDeliver` → `UseHandler` | [x] |
| B2 | Update InApp test callers | [x] |
| C1 | Add `EnsureAdapterAssembliesLoaded()` to scanner | [x] |
| D1 | Improve `ResolveAdapter` error message | [x] |
