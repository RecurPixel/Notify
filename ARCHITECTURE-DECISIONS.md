# RecurPixel.Notify — Architecture Decisions
## Auto-Registration & Config-Based Adapter Filtering

> Decisions made during beta.1 integration review. To be implemented in v0.1.0-beta.2.
> These two decisions are closely related — read both before implementing either.

---

## Decision 1 — Auto-Registration via Assembly Scanning

### What Changes

Explicit per-adapter registration calls are eliminated. Users no longer call `AddSmtpChannel()`, `AddSendGridChannel()`, `AddTwilioSmsChannel()` etc. in their setup.

`AddRecurPixelNotify` scans all loaded assemblies for types that implement `INotificationChannel`, reads a `[ChannelAdapter]` attribute to get the registration key, and registers them automatically — subject to Decision 2 (config-based filtering).

### Why

The explicit call model had a fundamental DX problem. Every adapter package ships with an `Add{Provider}Channel()` extension method. The XML doc on these methods previously (incorrectly) said they were called internally by `AddRecurPixelNotify`. Users read that, skipped the call, and hit a runtime DI failure with no obvious link back to setup.

More importantly, the goal of appsettings-driven configuration means the user should be able to change provider by editing config — not by editing `Program.cs`. If changing from SendGrid to Postmark requires both a config change and a code change, the library has failed at its own philosophy.

### The Constraint — Why Naive Type References Won't Work

`RecurPixel.Notify` (Core + Orchestrator) cannot reference adapter packages directly. Adapters depend on Core, not the other way around. If Core referenced `SmtpChannel` or `SendGridChannel` by type, the entire modular package structure collapses — every user would be forced to pull every adapter regardless of what they install.

Assembly scanning solves this. The adapters are already loaded into the AppDomain because the user installed and referenced them via NuGet. Core never needs to know their types at compile time — it discovers them at runtime.

### The `[ChannelAdapter]` Attribute

Defined in `RecurPixel.Notify.Core`. Every adapter applies it. The scanner reads it to build the DI registration key without instantiating the type.

```csharp
// Defined in RecurPixel.Notify.Core
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ChannelAdapterAttribute : Attribute
{
    /// <summary>Logical channel name — e.g. "email", "sms", "push", "slack"</summary>
    public string Channel { get; }

    /// <summary>
    /// Provider name for multi-provider channels — e.g. "sendgrid", "twilio", "fcm".
    /// Use "default" for single-implementation channels — e.g. "slack", "discord", "inapp".
    /// </summary>
    public string Provider { get; }

    public ChannelAdapterAttribute(string channel, string provider)
    {
        Channel  = channel;
        Provider = provider;
    }
}
```

**Applied to every adapter:**

```csharp
// Multi-provider channels — specific provider name
[ChannelAdapter("email", "sendgrid")]
public sealed class SendGridChannel : NotificationChannelBase { ... }

[ChannelAdapter("email", "smtp")]
public sealed class SmtpChannel : NotificationChannelBase { ... }

[ChannelAdapter("email", "postmark")]
public sealed class PostmarkChannel : NotificationChannelBase { ... }

[ChannelAdapter("sms", "twilio")]
public sealed class TwilioSmsChannel : NotificationChannelBase { ... }

[ChannelAdapter("sms", "vonage")]
public sealed class VonageSmsChannel : NotificationChannelBase { ... }

[ChannelAdapter("push", "fcm")]
public sealed class FcmChannel : NotificationChannelBase { ... }

[ChannelAdapter("push", "apns")]
public sealed class ApnsChannel : NotificationChannelBase { ... }

// Single-implementation channels — always "default"
[ChannelAdapter("slack", "default")]
public sealed class SlackChannel : NotificationChannelBase { ... }

[ChannelAdapter("discord", "default")]
public sealed class DiscordChannel : NotificationChannelBase { ... }

[ChannelAdapter("inapp", "default")]
public sealed class InAppChannel : NotificationChannelBase { ... }
```

### Scanner Implementation

```csharp
private static IEnumerable<(Type Type, ChannelAdapterAttribute Attr)> DiscoverAdapters()
{
    return AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic)
        .SelectMany(a =>
        {
            try { return a.GetTypes(); }
            catch (ReflectionTypeLoadException ex)
            { return ex.Types.Where(t => t != null)!; }
        })
        .Where(t => t is { IsAbstract: false, IsInterface: false }
                 && typeof(INotificationChannel).IsAssignableFrom(t))
        .Select(t => (Type: t, Attr: t.GetCustomAttribute<ChannelAdapterAttribute>()))
        .Where(x => x.Attr != null)!;
}
```

The `ReflectionTypeLoadException` guard handles partially loaded assemblies — without it the scan will crash on any assembly that has a missing dependency.

### Registration Key Convention

The key is always `"{channel}:{provider}"`:

| Channel | Provider | Registered Key    |
| ------- | -------- | ----------------- |
| email   | sendgrid | `email:sendgrid`  |
| email   | smtp     | `email:smtp`      |
| sms     | twilio   | `sms:twilio`      |
| push    | fcm      | `push:fcm`        |
| slack   | default  | `slack:default`   |
| discord | default  | `discord:default` |
| inapp   | default  | `inapp:default`   |

No special cases in the dispatcher. Every channel resolves the same way. Simple (single-implementation) channels always resolve to `:default`. Multi-provider channels resolve to `:configuredProvider` or `:namedProvider` from `Metadata["provider"]`.

### What the User's Setup Looks Like After This Change

```csharp
// Program.cs — complete notification setup, nothing else needed
builder.Services.AddRecurPixelNotify(
    builder.Configuration.GetSection("Notify"),
    orchestrator =>
    {
        orchestrator.DefineEvent(NotifyEvents.OrderPlaced, e => e
            .UseChannels("email", "sms", "inapp")
            .WithCondition("sms", ctx => ctx.User.PhoneVerified)
            .WithRetry(maxAttempts: 3, delayMs: 500)
        );

        orchestrator.OnDelivery<AppDbContext>(async (result, db) =>
        {
            await db.NotificationLogs.AddAsync(...);
            await db.SaveChangesAsync();
        });
    }
);

// InApp stays explicit — requires user-provided delivery handler
builder.Services.AddInAppChannel(inApp =>
{
    inApp.OnDeliver<AppDbContext>(async (notification, db) =>
    {
        await db.InboxItems.AddAsync(...);
        await db.SaveChangesAsync();
        return NotifyResult.Ok("inapp");
    });
});
```

No `AddSmtpChannel()`. No `AddSendGridChannel()`. Install the NuGet package, configure appsettings, done.

**InApp is the only explicit registration** — it cannot be automated because it requires user-provided application code (the delivery handler). Every other adapter is pure infrastructure and is fully driven by config.

### Assembly Loading Caveat

The user's app must reference the adapter package, not just install it, for the assembly to be loaded into AppDomain at scan time. In ASP.NET Core this is automatic for any `<PackageReference>` entry — no explicit `using` statement is needed, just the package reference. This should be called out in docs.

---

## Decision 2 — Config-Based Adapter Filtering

### What Changes

The scanner does not register every discovered adapter. It checks whether config exists for each one before registering it. An adapter with no config is silently skipped — it does not exist in the DI container at all.

### Why This Is Necessary

Without filtering, a user who installs `RecurPixel.Notify.Sdk` (all 30+ adapters) but configures only 3 providers gets a DI container full of broken registrations — adapters that will throw `NullReferenceException` or `InvalidOperationException` the moment they try to read their options. Startup validation cannot help because the validation runs per-channel, not per-unconfigured-adapter.

Config-based filtering means: **if you didn't configure it, it doesn't exist.** The mental model is identical to the original explicit registration model — the only difference is it's now automatic.

### `IsAdapterConfigured` Logic

Called by the scanner for every discovered adapter. Returns `true` only if the minimum required credential is present.

```csharp
private static bool IsAdapterConfigured(NotifyOptions options, string channel, string provider)
{
    return channel switch
    {
        "email" => provider switch
        {
            "sendgrid" => options.Email?.SendGrid?.ApiKey        is not null,
            "smtp"     => options.Email?.Smtp?.Host              is not null,
            "mailgun"  => options.Email?.Mailgun?.ApiKey         is not null,
            "resend"   => options.Email?.Resend?.ApiKey          is not null,
            "postmark" => options.Email?.Postmark?.ApiKey        is not null,
            "awsses"   => options.Email?.AwsSes?.AccessKey       is not null,
            _          => false
        },
        "sms" => provider switch
        {
            "twilio"      => options.Sms?.Twilio?.AccountSid     is not null,
            "vonage"      => options.Sms?.Vonage?.ApiKey         is not null,
            "plivo"       => options.Sms?.Plivo?.AuthId          is not null,
            "sinch"       => options.Sms?.Sinch?.ApiKey          is not null,
            "messagebird" => options.Sms?.MessageBird?.ApiKey    is not null,
            "awssns"      => options.Sms?.AwsSns?.AccessKey      is not null,
            _             => false
        },
        "push" => provider switch
        {
            "fcm"       => options.Push?.Fcm?.ProjectId          is not null,
            "apns"      => options.Push?.Apns?.KeyId             is not null,
            "onesignal" => options.Push?.OneSignal?.AppId        is not null,
            "expo"      => options.Push?.Expo?.AccessToken       is not null,
            _           => false
        },
        "whatsapp" => provider switch
        {
            "twilio"    => options.WhatsApp?.Twilio?.AccountSid  is not null,
            "metacloud" => options.WhatsApp?.MetaCloud?.PhoneNumberId is not null,
            "vonage"    => options.WhatsApp?.Vonage?.ApiKey      is not null,
            _           => false
        },

        // Simple channels — check the minimum credential on the top-level options object
        "slack"    => options.Slack?.WebhookUrl                  is not null,
        "discord"  => options.Discord?.WebhookUrl                is not null,
        "teams"    => options.Teams?.WebhookUrl                  is not null,
        "telegram" => options.Telegram?.BotToken                 is not null,
        "facebook" => options.Facebook?.PageAccessToken          is not null,
        "line"     => options.Line?.ChannelAccessToken           is not null,
        "viber"    => options.Viber?.AuthToken                   is not null,

        // InApp — always registered; handler is wired separately via AddInAppChannel
        "inapp"    => true,

        _ => false
    };
}
```

### Full Scanner + Filter Flow

```csharp
internal static List<string> RegisterAdapters(
    IServiceCollection services,
    NotifyOptions options,
    ILogger? logger = null)
{
    var registeredKeys = new List<string>();

    foreach (var (type, attr) in DiscoverAdapters())
    {
        var key = $"{attr.Channel}:{attr.Provider}";

        if (!IsAdapterConfigured(options, attr.Channel, attr.Provider))
        {
            logger?.LogDebug(
                "Skipping adapter {AdapterType} — no config found for {Key}.",
                type.Name, key);
            continue;
        }

        services.AddKeyedSingleton(typeof(INotificationChannel), key, type);
        registeredKeys.Add(key);

        logger?.LogDebug(
            "Registered adapter {AdapterType} as {Key}.",
            type.Name, key);
    }

    return registeredKeys; // returned for startup validation
}
```

### Startup Validation After Scanning

Run after the scan to catch the case where `Provider` points at an adapter whose credentials were not provided:

```csharp
private static void ValidateActiveProviders(NotifyOptions options, List<string> registeredKeys)
{
    void AssertRegistered(string? provider, string channel)
    {
        if (provider == null) return;
        var key = $"{channel}:{provider}";
        if (!registeredKeys.Contains(key))
            throw new InvalidOperationException(
                $"Notify:{ToPascalCase(channel)}:Provider is set to '{provider}' " +
                $"but no credentials were found for it. " +
                $"Add Notify:{ToPascalCase(channel)}:{ToPascalCase(provider)} " +
                $"to your configuration.");
    }

    AssertRegistered(options.Email?.Provider,    "email");
    AssertRegistered(options.Sms?.Provider,      "sms");
    AssertRegistered(options.Push?.Provider,     "push");
    AssertRegistered(options.WhatsApp?.Provider, "whatsapp");

    // Named providers
    foreach (var (name, def) in options.Email?.Providers ?? new())
    {
        var key = $"email:{def.Type}";
        if (!registeredKeys.Contains(key))
            throw new InvalidOperationException(
                $"Notify:Email:Providers['{name}'].Type is '{def.Type}' " +
                $"but no credentials were found for it.");
    }
    // ... same for Sms, Push, WhatsApp named providers
}
```

### Behaviour Walkthrough

**Scenario: User installs `RecurPixel.Notify.Sdk` (30+ adapters), configures 3.**

```json
"Notify": {
  "Email": {
    "Provider": "sendgrid",
    "SendGrid": { "ApiKey": "SG.xxx", "FromEmail": "no-reply@example.com" }
  },
  "Sms": {
    "Provider": "twilio",
    "Twilio": { "AccountSid": "ACxxx", "AuthToken": "xxx", "FromNumber": "+1..." }
  },
  "Slack": {
    "WebhookUrl": "https://hooks.slack.com/services/xxx"
  }
}
```

Scanner discovers 30+ adapter types. `IsAdapterConfigured` passes for 3:

```
✅  email:sendgrid    — SendGrid.ApiKey present
✅  sms:twilio        — Twilio.AccountSid present
✅  slack:default     — Slack.WebhookUrl present
⏭  email:smtp        — Smtp.Host null, skipped
⏭  email:postmark    — Postmark.ApiKey null, skipped
⏭  sms:vonage        — Vonage.ApiKey null, skipped
⏭  push:fcm          — Fcm.ProjectId null, skipped
... (27 more skipped)
```

DI container has exactly 3 `INotificationChannel` registrations. Startup validation passes. Everything else is invisible to the application.

**Scenario: User sets `Provider: "sendgrid"` but omits credentials.**

```json
"Email": {
  "Provider": "sendgrid"
}
```

Scanner finds `SendGridChannel`, `IsAdapterConfigured` returns `false` (ApiKey is null), skips registration. `ValidateActiveProviders` then sees `Email.Provider = "sendgrid"` but `"email:sendgrid"` is not in `registeredKeys`. Throws at startup:

```
InvalidOperationException: Notify:Email:Provider is set to 'sendgrid'
but no credentials were found for it.
Add Notify:Email:SendGrid to your configuration.
```

Clear startup error. Never a silent runtime failure.

### Consistency with Integration Testing

The original integration test design already used config presence to skip tests — if no credentials, the test is skipped. This decision extends the exact same model to production registration. The user mental model is identical in both environments: **configure it to use it, don't configure it and it doesn't exist.**

---

## Combined Flow — Startup to First Send

```
AddRecurPixelNotify(config, orchestrator => { ... })
    │
    ├── 1. Bind NotifyOptions from IConfiguration
    ├── 2. Register IOptions<NotifyOptions> and NotifyOptions in DI
    ├── 3. DiscoverAdapters() — scan AppDomain assemblies for [ChannelAdapter]
    ├── 4. IsAdapterConfigured() — filter by config presence
    ├── 5. RegisterAdapters() — keyed DI registration for passing adapters only
    ├── 6. ValidateActiveProviders() — fail fast if Provider points at missing credentials
    └── 7. Wire Orchestrator, OnDelivery handlers, event registry

AddInAppChannel(inApp => { inApp.OnDeliver<DbContext>(...) })
    └── Registers InApp delivery handler separately (user-provided application code)

---

TriggerAsync("order.placed", context)
    ├── Look up event definition in registry
    ├── For each channel in event:
    │   ├── Evaluate condition — skip if false
    │   ├── ResolveKey: Metadata["provider"] → configured Provider → :default
    │   ├── GetRequiredKeyedService<INotificationChannel>(key)
    │   ├── SendAsync(payload) — with retry
    │   └── OnDelivery hook fires with NotifyResult
    └── Return TriggerResult { ChannelResults }
```

---

## Impact on BETA2-PLAN.md

These decisions affect the following items in the beta.2 checklist:

**Replaces:**
- "Fix XML docs on all `Add{Channel}` extension methods" 
→ Add{Provider}Channel() methods are retained for Tier 1 direct-injection usage. Their internal implementation changes — they no longer register the adapter (scanning handles that) and instead add a non-keyed INotificationChannel alias pointing at the already-registered keyed service. XML docs updated to reflect this.

**Adds to Blocking Bugs:**
- Add `[ChannelAdapter]` attribute to `RecurPixel.Notify.Core`
- Apply `[ChannelAdapter]` attribute to all 30+ adapter classes
- Implement `DiscoverAdapters()` scanner in `AddRecurPixelNotify`
- Implement `IsAdapterConfigured()` config filter
- Implement `ValidateActiveProviders()` startup validation
- Remove all `Add{Provider}Channel()` extension methods (or mark `[Obsolete]` with clear message for a one-version transition period)

**Adds to Documentation:**
- Document assembly loading requirement (PackageReference sufficient, no explicit using needed)
- Document that InApp is the only channel requiring an explicit registration call and why
- Update all quickstart examples to remove per-adapter registration calls

---

*RecurPixel.Notify — Architecture Decisions. March 2026.*
