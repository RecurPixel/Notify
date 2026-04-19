# RecurPixel.Notify — Roadmap

> Last updated: April 2026
> Current stable: v0.2.0
> In progress: v0.3.0-beta

---

## What ships in each version

| Version | Headline | Status |
|---|---|---|
| v0.2.0 | 35 adapters across all channels | Stable |
| v0.3.0 | Dashboard observability + MSG91 + NotifyResult improvements | In progress |
| v0.4.0 | Polly hooks, OpenTelemetry, circuit breaker, Dashboard v2 | Planned |

---

## v0.3.0

### What's in scope

**NotifyResult improvements (Phase 14)**

The smallest change with the biggest practical impact. Every `NotifyResult` and `BulkNotifyResult` now carries:

```csharp
public class NotifyResult
{
    // ... existing fields unchanged ...

    // NEW in v0.3.0
    public string?  EventName    { get; set; }  // which event triggered this send
    public string?  BulkBatchId  { get; set; }  // groups all results from one BulkTriggerAsync call
    public Dictionary<string, object>? Metadata { get; set; }  // passthrough from NotifyContext
}
```

`BulkBatchId` is a `Guid` generated once per `BulkTriggerAsync` call and stamped on every individual result in that batch. This is what makes the dashboard's "view all in this batch" feature possible.

`EventName` and `Metadata` passthrough means your `OnDelivery` hook now has full context — you can log which event fired, correlate with your own request IDs via metadata, and trace a notification back to the action that caused it.

No breaking changes. Existing `OnDelivery` handlers continue to work without modification.

---

**Dashboard — `RecurPixel.Notify.Dashboard` (Phase 15 + 16)**

Two new packages:

```
RecurPixel.Notify.Dashboard         ← middleware, UI, INotificationLogStore interface
RecurPixel.Notify.Dashboard.EfCore  ← EF Core implementation, IEntityTypeConfiguration, migration
```

Both are added to `RecurPixel.Notify.Sdk`.

**What the dashboard tracks**

Every send attempt is logged automatically once you install the dashboard packages. No changes to your existing `TriggerAsync` or `OnDelivery` code.

```csharp
public class NotificationLog
{
    public long      Id            { get; set; }
    public string    Channel       { get; set; }   // "email", "sms", "push" etc
    public string    Provider      { get; set; }   // "sendgrid", "twilio", "msg91" etc
    public string    Recipient     { get; set; }   // To field — email, phone, device token
    public string?   Subject       { get; set; }   // null for channels with no subject
    public string?   EventName     { get; set; }   // which event fired
    public bool      Success       { get; set; }
    public string?   ProviderId    { get; set; }   // provider's own message ID
    public string?   Error         { get; set; }   // populated on failure
    public bool      IsBulk        { get; set; }
    public string?   BulkBatchId   { get; set; }   // groups a bulk send together
    public bool      UsedFallback  { get; set; }
    public string?   NamedProvider { get; set; }   // set when named routing was used
    public DateTime  SentAt        { get; set; }
}
```

**Registration**

```csharp
// Program.cs
builder.Services.AddNotifyDashboard(options =>
{
    options.RoutePrefix  = "notify-dashboard";   // default — dashboard at /notify-dashboard
    options.PageTitle    = "Notifications";      // browser tab title
    options.RequireRole  = "Admin";              // null = open (dev only, warns in production)
    options.PolicyName   = null;                 // use an existing named auth policy instead
    options.PageSize     = 50;
});

// If using the built-in EF Core store
builder.Services.AddNotifyDashboardEfCore(
    builder.Configuration.GetConnectionString("Default")
);

// Middleware
app.UseNotifyDashboard();
```

**Bring your own store**

If you're not using EF Core, implement `INotificationLogStore` directly:

```csharp
public interface INotificationLogStore
{
    Task AddAsync(NotificationLog log, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationLog>> QueryAsync(NotificationLogQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationLog>> GetBatchAsync(string bulkBatchId, CancellationToken ct = default);
}

// Register your implementation
builder.Services.AddNotifyDashboard(options => { ... });
builder.Services.AddSingleton<INotificationLogStore, MyDapperLogStore>();
```

**Migration (EF Core path)**

The `EfCore` package ships an `IEntityTypeConfiguration<NotificationLog>` you apply in two ways:

```csharp
// Option A — standalone context (own connection string, own migration)
// Run: dotnet ef migrations add InitDashboard --context NotifyDashboardDbContext

// Option B — plug into your existing DbContext
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);
    builder.AddNotifyDashboard();  // adds NotificationLog to your existing schema
}
```

Option B is what most real projects will use. One DB, one migration run.

**What the UI shows**

Single page. No framework. Served as embedded HTML from the middleware, pattern identical to Hangfire's dashboard.

- Summary row: total sent today, success rate, failure count, active channels
- Log table: time, channel (badge), provider, recipient, subject (truncated), status (badge), bulk indicator
- Expanding bulk rows: click any bulk row to see all recipients in that batch grouped together
- Failed row detail: click to see full error text and provider response
- Filters: channel, status (all/success/failed), date range, recipient search

**What the dashboard does NOT do in v0.3.0**

- No retry actions (planned for v0.4.0 — requires careful design)
- No delete/archive
- No config editing
- No real-time streaming

---

**MSG91 adapter (Phase 17)**

```
RecurPixel.Notify.Sms.Msg91        ← SMS via MSG91
RecurPixel.Notify.WhatsApp.Msg91   ← WhatsApp Business via MSG91
```

Both use the same API key. Same registration pattern as all other adapters.

```json
"Sms": {
  "Provider": "msg91",
  "Msg91": {
    "AuthKey":  "your-auth-key",
    "SenderId": "NOTIFY",
    "Route":    "4"
  }
},
"WhatsApp": {
  "Provider": "msg91",
  "Msg91": {
    "AuthKey":       "your-auth-key",
    "IntegratedNumber": "+91XXXXXXXXXX",
    "Namespace":     "your-namespace"
  }
}
```

MSG91 has no native bulk SMS API — the base class loop handles bulk automatically.

---

### What is NOT in v0.3.0

| Feature | Why not | Where it goes |
|---|---|---|
| Circuit breaker | Needs its own design — per-provider state, closed/open/half-open, reset timers | v0.4.0 |
| OpenTelemetry | Clean add-on, no rush | v0.4.0 |
| Scheduled send | Requires background infrastructure — contradicts "we don't ship a queue" | Separate design doc first |
| Template engine | Contradicts core philosophy — user owns content | Never |
| Dashboard retry actions | Dashboard needs to stabilise before adding write operations | v0.4.0 |

**For scheduled sends today:** Use Hangfire or Quartz.NET. Call `TriggerAsync` from your background job. RecurPixel.Notify does not ship a scheduler and never will.

**For circuit breaking today:** Wrap `TriggerAsync` in a Polly `ResiliencePipeline`. This is ~30 lines in your own service layer and requires no library support. Example:

```csharp
// In your service
private readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder()
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio          = 0.5,
        SamplingDuration      = TimeSpan.FromSeconds(30),
        MinimumThroughput     = 5,
        BreakDuration         = TimeSpan.FromSeconds(60),
    })
    .Build();

public async Task SendAsync(string eventName, NotifyContext ctx)
    => await _pipeline.ExecuteAsync(ct => _notify.TriggerAsync(eventName, ctx, ct));
```

---

### v0.3.0 build order

| Phase | Deliverable | Dependency |
|---|---|---|
| 14 | `NotifyResult` — add `EventName`, `BulkBatchId`, `Metadata` | None — start here |
| 15 | Dashboard data layer — `NotificationLog`, `INotificationLogStore`, `Dashboard.EfCore` | Phase 14 (`BulkBatchId` must exist first) |
| 16 | Dashboard UI — middleware, embedded HTML, REST API, auth | Phase 15 (needs query layer) |
| 17 | MSG91 adapters — `Sms.Msg91`, `WhatsApp.Msg91` | None — parallel with 15/16 |

---

## v0.4.0

### What's planned

**Polly hooks (Phase 18)**

Every HTTP-based adapter exposes an `IHttpClientBuilder` hook so you can attach Polly policies at registration time without the library needing to own resilience logic.

```csharp
builder.Services.AddRecurPixelNotify(options => { ... })
    .ConfigureEmailHttpClient(http => http
        .AddResiliencePipeline("email", builder => builder
            .AddRetry(...)
            .AddCircuitBreaker(...)));
```

This is the right division of responsibility — you own the resilience policy, the library owns the HTTP client lifetime.

---

**OpenTelemetry (Phase 19)**

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddRecurPixelNotifyInstrumentation());
```

Emits one `Activity` per send attempt with tags: `notify.channel`, `notify.provider`, `notify.event_name`, `notify.success`, `notify.recipient_hash` (hashed, not raw PII).

---

**Circuit breaker (Phase 20)**

First-class circuit breaker inside the Orchestrator. Per-provider state tracking, configurable failure threshold, automatic half-open probe after cooldown.

```csharp
options.CircuitBreaker(cb => cb
    .FailureThreshold(5)          // 5 failures in window
    .SamplingWindow(TimeSpan.FromMinutes(1))
    .BreakDuration(TimeSpan.FromMinutes(5))
    .OnBreak(ctx => logger.LogWarning("Provider {Provider} circuit open", ctx.Provider))
    .OnReset(ctx => logger.LogInformation("Provider {Provider} circuit reset", ctx.Provider))
);
```

When a provider's circuit is open, the Orchestrator skips it and immediately tries the configured fallback. You can query circuit state via `INotifyService.GetCircuitStateAsync(channel, provider)`.

---

**Additional adapters (Phase 21)**

Candidates based on adoption and testability:
- `Sms.Kaleyra` — strong India coverage
- `WhatsApp.Gupshup` — widely used in India, free sandbox
- `WhatsApp.AiSensy` — popular managed WhatsApp BSP
- Any community-submitted adapters that pass integration tests

---

**Dashboard v2 (Phase 22)**

- Retry actions — resend a failed notification from the dashboard
- Batch detail page — dedicated view for a bulk batch with per-recipient status
- Provider health indicators — success rate per provider over the last 24h shown in the summary row
- Export — download filtered results as CSV

---

## What will never ship in this library

| Feature | Reason |
|---|---|
| Template engine | Core philosophy: "we deliver the payload, we do not build it" |
| Queue / background dispatcher | Core philosophy: "user calls TriggerAsync from their own worker" |
| User preference storage | User owns this — passed via conditions at event definition time |
| Notification log storage (without Dashboard) | User owns persistence — `OnDelivery` hook exists for this |
| Load balancing / round-robin across providers | Over-engineering for a notification library |
| A/B testing across providers | Same |

---

## Upgrading

### v0.2.0 → v0.3.0

No breaking changes. All existing code compiles without modification.

New optional fields on `NotifyResult` default to `null` — existing `OnDelivery` handlers work unchanged.

To opt into dashboard logging:

1. Install `RecurPixel.Notify.Dashboard` and `RecurPixel.Notify.Dashboard.EfCore`
2. Call `AddNotifyDashboard()` and `AddNotifyDashboardEfCore()` in `Program.cs`
3. Call `app.UseNotifyDashboard()`
4. Run migrations (`dotnet ef migrations add AddNotifyDashboard`)
5. Done — logs appear automatically

---

*RecurPixel.Notify — Roadmap. April 2026.*
