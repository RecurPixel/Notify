# RecurPixel.Notify — Bulk & batch design rules

> Read this file for any phase involving SendBulkAsync, BulkNotifyResult, or BulkTriggerAsync.

---

## Core decision

Bulk is a second method on the same interface. Single-send contracts do not change.

`NotificationPayload` — unchanged.
`NotifyResult` — unchanged.
`INotificationChannel` — has both `SendAsync` and `SendBulkAsync` from day one.
`INotifyService` — has both `TriggerAsync` and `BulkTriggerAsync`.

Adapters with native batch APIs override `SendBulkAsync`.
Adapters without get a default loop from `NotificationChannelBase` — they do not override it.
User code does not know or care which path ran.

---

## Updated INotificationChannel

```csharp
public interface INotificationChannel
{
    string ChannelName { get; }

    /// <summary>Single recipient send.</summary>
    Task<NotifyResult> SendAsync(NotificationPayload payload, CancellationToken ct = default);

    /// <summary>
    /// Multiple recipient send. Providers with native batch APIs override this.
    /// Default implementation loops SendAsync — behaviour is identical either way.
    /// </summary>
    Task<BulkNotifyResult> SendBulkAsync(IReadOnlyList<NotificationPayload> payloads, CancellationToken ct = default);
}
```

---

## NotificationChannelBase — default bulk implementation

```csharp
public abstract class NotificationChannelBase : INotificationChannel
{
    public abstract string ChannelName { get; }
    public abstract Task<NotifyResult> SendAsync(NotificationPayload payload, CancellationToken ct = default);

    // Default: loops SendAsync with concurrency cap. Never reimplement this in adapters.
    public virtual async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads, CancellationToken ct = default)
    {
        var semaphore = new SemaphoreSlim(BulkConcurrencyLimit);
        var tasks = payloads.Select(async payload =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var result = await SendAsync(payload, ct);
                result.Recipient = payload.To;
                return result;
            }
            finally { semaphore.Release(); }
        });

        var results = await Task.WhenAll(tasks);
        return new BulkNotifyResult
        {
            Results         = results,
            Channel         = ChannelName,
            UsedNativeBatch = false
        };
    }

    protected virtual int BulkConcurrencyLimit => 10;
}
```

---

## BulkNotifyResult

```csharp
public class BulkNotifyResult
{
    public IReadOnlyList<NotifyResult> Results         { get; init; }
    public bool   AllSucceeded    => Results.All(r => r.Success);
    public bool   AnySucceeded    => Results.Any(r => r.Success);
    public IReadOnlyList<NotifyResult> Failures        => Results.Where(r => !r.Success).ToList();
    public int    Total           => Results.Count;
    public int    SuccessCount    => Results.Count(r => r.Success);
    public int    FailureCount    => Results.Count(r => !r.Success);
    public string Channel         { get; init; }
    public bool   UsedNativeBatch { get; init; }
}
```

---

## INotifyService — bulk additions

```csharp
public interface INotifyService
{
    // Orchestrated single send
    Task<NotifyResult> TriggerAsync(string eventName, NotifyContext context, CancellationToken ct = default);

    // Orchestrated bulk send — each NotifyContext is one user with their own payloads
    Task<BulkNotifyResult> BulkTriggerAsync(string eventName, IReadOnlyList<NotifyContext> contexts, CancellationToken ct = default);

    // Direct channel access — bypasses orchestration
    INotificationChannel Email    { get; }
    INotificationChannel Sms      { get; }
    INotificationChannel Push     { get; }
    // ... etc
}
```

---

## BulkOptions configuration

```csharp
public class BulkOptions
{
    public int  ConcurrencyLimit { get; set; } = 10;    // parallel sends in loop path
    public int  MaxBatchSize     { get; set; } = 1000;  // ceiling for native batch API calls
    public bool AutoChunk        { get; set; } = true;  // auto-chunk if over MaxBatchSize
}
```

```json
"Notify": {
  "Bulk": {
    "ConcurrencyLimit": 10,
    "MaxBatchSize": 1000,
    "AutoChunk": true
  }
}
```

---

## Adapter implementation patterns

### Pattern A — no native bulk (no override needed)

```csharp
public class SmtpChannel : NotificationChannelBase
{
    public override string ChannelName => "email";

    public override async Task<NotifyResult> SendAsync(NotificationPayload payload, CancellationToken ct)
    {
        // single SMTP send
    }

    // No SendBulkAsync override — base class loop handles it automatically
}
```

### Pattern B — native batch API

```csharp
public class SendGridChannel : NotificationChannelBase
{
    public override string ChannelName => "email";

    public override async Task<NotifyResult> SendAsync(NotificationPayload payload, CancellationToken ct)
    {
        // single send
    }

    public override async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads, CancellationToken ct = default)
    {
        var chunks = payloads.Chunk(_bulkOptions.MaxBatchSize);
        var allResults = new List<NotifyResult>();

        foreach (var chunk in chunks)
        {
            // call SendGrid batch endpoint
            // map response back to individual NotifyResult per recipient
            allResults.AddRange(mapped);
        }

        return new BulkNotifyResult { Results = allResults, Channel = ChannelName, UsedNativeBatch = true };
    }
}
```

### Pattern C — FCM multicast (500 tokens per call)

```csharp
public class FcmChannel : NotificationChannelBase
{
    public override string ChannelName => "push";

    public override async Task<BulkNotifyResult> SendBulkAsync(
        IReadOnlyList<NotificationPayload> payloads, CancellationToken ct = default)
    {
        var chunks = payloads.Chunk(500);  // FCM hard limit
        var allResults = new List<NotifyResult>();

        foreach (var chunk in chunks)
        {
            // extract device tokens, call FCM MulticastAsync
            // map batch response to individual NotifyResult per token
            allResults.AddRange(mapped);
        }

        return new BulkNotifyResult { Results = allResults, Channel = ChannelName, UsedNativeBatch = true };
    }
}
```

---

## Delivery hook — called per individual result

`OnDelivery` is called once per `NotifyResult`, not once per `BulkNotifyResult`.
The Orchestrator iterates `BulkNotifyResult.Results` and calls `OnDelivery` for each.
Existing single-send hook handlers work unchanged for bulk sends.

```csharp
options.OnDelivery(async result =>
{
    // called for every NotifyResult — single and bulk use the same handler
    await db.NotificationLogs.AddAsync(new NotificationLog
    {
        Recipient  = result.Recipient,
        Channel    = result.Channel,
        Provider   = result.Provider,
        Status     = result.Success ? "sent" : "failed",
        ProviderId = result.ProviderId,
        Error      = result.Error,
        SentAt     = result.SentAt
    });
});
```

---

## Native bulk support — adapter decision table

| Channel | Provider | Override SendBulkAsync? | Limit |
|---|---|---|---|
| Email | SendGrid | Yes | 1000/call |
| Email | AwsSes | Yes | batch API |
| Email | Postmark | Yes | batch endpoint |
| Email | Mailgun | Yes | recipient variables |
| Email | Resend | No | no batch API |
| Email | SMTP | No | protocol is single send |
| SMS | Twilio | No | no batch API |
| SMS | Vonage | Yes | bulk SMS API |
| SMS | AwsSns | Yes | topic publish |
| SMS | Sinch | Yes | batch SMS API |
| SMS | MSG91 | No | no batch API |
| Push | FCM | Yes | 500 tokens/call |
| Push | APNs | No | one per call |
| Push | OneSignal | Yes | bulk notifications API |
| Push | Expo | Yes | push tickets batch |
| WhatsApp | Any | No | Meta policy restricts bulk |
| Slack | — | No | one per webhook |
| Discord | — | No | one per webhook |
| Teams | — | No | one per webhook |
| Telegram | — | No | no bulk DM |
| Facebook | — | No | per-user Messenger API |

Adapters marked No: do NOT override SendBulkAsync — base class loop is correct.

---

## Rules summary

- `INotificationChannel` has both `SendAsync` and `SendBulkAsync` — both present from day one
- Every adapter extends `NotificationChannelBase`, not `INotificationChannel` directly
- `NotificationChannelBase` provides the default loop — never reimplement it
- Only override `SendBulkAsync` if the provider has a native batch API
- `BulkNotifyResult.Results` is a flat list — one `NotifyResult` per input payload, same order
- Set `UsedNativeBatch = true` only when the adapter's own batch API was called
- Always chunk payloads before calling native batch APIs (FCM: 500, SendGrid: 1000)
- `OnDelivery` is called per individual `NotifyResult`, not per `BulkNotifyResult`
- Always set `NotifyResult.Recipient = NotificationPayload.To` in bulk results
- Never run all payloads concurrently with no limit — always use `SemaphoreSlim`

---

*RecurPixel.Notify — Bulk & batch design. Updated: April 2026.*
