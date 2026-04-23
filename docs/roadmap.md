---
layout: default
title: Roadmap
nav_order: 10
---

# Roadmap

---

## v0.4.0 — Planned (mid-2026)

### Phase 18 — Polly Resilience Hooks

Opt-in Polly policy integration per adapter. Every HTTP-based adapter will expose an `IHttpClientBuilder` hook so you can attach your own Polly policies — retry, timeout, circuit breaker, bulkhead — without any Polly dependency inside the library.

```csharp
// Planned API — subject to change before release
builder.Services.AddRecurPixelNotify(options => { ... })
    .ConfigureEmailHttpClient(http => http
        .AddResiliencePipeline("email", pipeline => pipeline
            .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3 })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio    = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration   = TimeSpan.FromSeconds(60),
            })
            .AddTimeout(TimeSpan.FromSeconds(10))));
```

**What this means:** you own the resilience policy entirely. The library owns the `HttpClient` lifetime and named client wiring. No Polly dependency in `RecurPixel.Notify.Core` — you bring Polly if you want it.

**Circuit breaking today (no library needed):** Wrap `TriggerAsync` in a Polly `ResiliencePipeline` in your own service layer. It's roughly 10–15 lines and gives you full control over failure thresholds, sampling windows, and break duration.

---

### Phase 19 — OpenTelemetry Integration

Full distributed tracing via `System.Diagnostics.ActivitySource`. Every `TriggerAsync` call, channel dispatch, and provider API call becomes a traceable span.

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddRecurPixelNotifyInstrumentation());
```

**Activity tags per send attempt:**

| Tag | Value |
|-----|-------|
| `notify.channel` | `"email"`, `"sms"`, `"push"`, etc. |
| `notify.provider` | `"sendgrid"`, `"twilio"`, etc. |
| `notify.event_name` | Event name from `TriggerAsync` |
| `notify.success` | `"true"` or `"false"` |
| `notify.recipient_hash` | SHA-256 first 8 chars — never raw PII |
| `notify.used_fallback` | `"true"` when a fallback channel fired |
| `notify.bulk_batch_id` | Batch ID when part of a `BulkTriggerAsync` call |

Exportable to OTLP, Zipkin, Jaeger, or any OpenTelemetry collector. No `Activity` is emitted if no listener is registered — zero overhead when OpenTelemetry is not configured.

{: .note }
> **Privacy:** `notify.recipient_hash` is a one-way hash — raw email addresses, phone numbers, and device tokens are never written to traces.

---

### Phase 20 — Additional Adapters

New provider adapters based on adoption:

| Package | Provider | Channel |
|---|---|---|
| `RecurPixel.Notify.Sms.Kaleyra` | Kaleyra | SMS |
| `RecurPixel.Notify.WhatsApp.Gupshup` | Gupshup | WhatsApp |
| `RecurPixel.Notify.WhatsApp.AiSensy` | AiSensy | WhatsApp |

Community contributions welcome. Requirements: extend `NotificationChannelBase`, include a full unit test suite, provide a `SkippableFact` integration test that skips when credentials are absent. See [Contributing](https://github.com/RecurPixel/Notify/blob/main/CONTRIBUTING.md).

---

### Phase 21 — Dashboard v2

Read-only enhancements to the [Dashboard](dashboard) introduced in v0.3.0:

- **Batch detail page** — dedicated view for a single bulk send with per-recipient status rows
- **Provider health indicators** — per-provider success rate over the last 24h in the summary row
- **CSV export** — download filtered log results from the UI

{: .warning }
> **Retry actions are not in v0.4.0.** Resending a failed notification from the dashboard requires storing the original payload (not currently persisted) and solving idempotency. This needs a design document before any implementation.

---

## What Will Never Be Built

These are explicitly out of scope. Build them in your application layer if you need them.

| Feature | Why not |
|---|---|
| **Template engine** | You own subject, body, and HTML. We deliver the payload, we don't build it. |
| **Background queue / dispatcher** | Call `TriggerAsync` from your own Hangfire, Quartz, or MassTransit job. |
| **Scheduled send** | Requires a persistent scheduler — wire your own, call `TriggerAsync` at fire time. |
| **First-class circuit breaker** | Phase 18 (Polly hooks) gives you a full circuit breaker in ~15 lines using Polly's own `CircuitBreakerStrategy`. Building a duplicate inside the library adds stateful infrastructure and duplicates work Polly already does better. |
| **Load balancing / round-robin across providers** | Use Named Provider Routing for intent-based routing; random distribution across providers is not a notification library concern. |
| **A/B testing across providers** | Same reason. |
| **Notification log storage (without Dashboard)** | `OnDelivery` hook exists; you write to your own DB. The Dashboard package is opt-in for those who want a built-in store. |
