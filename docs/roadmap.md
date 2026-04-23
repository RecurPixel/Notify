---
layout: default
title: Roadmap
nav_order: 10
---

# Roadmap

---

## v0.4.0 — Planned (mid-2026)

### Phase 18 — Polly Resilience Hooks

Opt-in Polly policy integration per channel. Configure retry, circuit-breaker, timeout, and bulkhead policies without coupling your app code to Polly.

```csharp
// Per-event policy wiring (planned API — subject to change)
orchestratorOptions.DefineEvent("auth.otp", e => e
    .UseChannels("sms")
    .WithPolicy(Policy
        .Handle<HttpRequestException>()
        .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(Math.Pow(2, i))))
);
```

- Per-channel policy or global default
- Works alongside existing `WithRetry` — Polly takes over when configured
- No Polly dependency in Core; opt-in via a separate `RecurPixel.Notify.Polly` package

---

### Phase 19 — OpenTelemetry Integration

Full distributed tracing via `System.Diagnostics.ActivitySource`. Every `TriggerAsync` call, channel dispatch, and provider API call becomes a traceable span.

- `ActivitySource` named `RecurPixel.Notify`
- Spans: `notify.trigger`, `notify.channel.dispatch`, `notify.provider.send`
- Attributes: `notify.event`, `notify.channel`, `notify.provider`, `notify.success`
- Exportable to OTLP, Zipkin, Jaeger, or any OpenTelemetry collector
- Zero-config opt-in via `AddOpenTelemetry().WithTracing(b => b.AddRecurPixelNotifyInstrumentation())`

---

### Phase 20 — Circuit Breaker

Auto-disable a channel after consecutive failures. Prevents hammering a broken provider and lets the fallback chain take over automatically.

- Configurable failure threshold and recovery window per channel
- State: Closed → Open → Half-Open (same model as Polly's circuit breaker)
- Dashboard shows tripped channels with open-since timestamp
- No code changes required to benefit — purely configuration

```json
"Notify": {
  "CircuitBreaker": {
    "FailureThreshold": 5,
    "RecoveryWindowSeconds": 60
  }
}
```

---

### Phase 21 — Additional Adapters

Planned new providers:

| Package                         | Provider             | Channel |
| ------------------------------- | -------------------- | ------- |
| `RecurPixel.Notify.Sms.Infobip` | Infobip              | SMS     |
| `RecurPixel.Notify.Email.Brevo` | Brevo (Sendinblue)   | Email   |
| `RecurPixel.Notify.Sms.Pinpoint`| AWS Pinpoint         | SMS     |

Community contributions welcome — see [Contributing](https://github.com/RecurPixel/Notify/blob/main/CONTRIBUTING.md).

---

### Phase 22 — Dashboard v2

Enhancements to the v0.3.0 Dashboard:

- **Real-time delivery feed** via SignalR — notifications appear live without page refresh
- **Per-provider latency charts** — p50/p95/p99 send time over a rolling 24-hour window
- **Failure rate alerts** — configurable threshold triggers a configurable webhook
- **Log retention policy** — auto-prune records older than N days
- **CSV export** — export filtered log results to CSV from the UI

---

## What Will Never Be Built

These are explicitly out of scope. If you need them, build them in your application layer:

- **Template engine** — you own subject, body, and HTML. We deliver the payload, we don't build it.
- **Background queue / dispatcher** — call `TriggerAsync` from your own Hangfire, Quartz, or MassTransit job.
- **Scheduled send** — a `SendAt` field requires a persistent scheduler. Wire your own.
- **Load balancing or A/B testing** across providers — use Named Provider Routing for routing by intent, not volume.
- **Log storage in Core** — `OnDelivery` hook exists; you write to your own DB (or use the optional Dashboard package).
