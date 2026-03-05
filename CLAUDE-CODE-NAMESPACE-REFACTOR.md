# Claude Code Instructions — Namespace Reorganization
## RecurPixel.Notify — v0.2.0 Namespace Cleanup

> Feed this entire file to Claude Code as the first message in a new session.
> Do not combine this task with any other changes. Do this on a clean branch.

---

## Context

We are reorganizing namespaces in RecurPixel.Notify. No logic changes, no new features, no bug fixes. This is purely a namespace and using-statement cleanup. Every test that passed before must still pass after.

The solution structure is:
```
src/
  RecurPixel.Notify.Core/
  RecurPixel.Notify.Orchestrator/
  RecurPixel.Notify.Email.SendGrid/
  RecurPixel.Notify.Email.Smtp/
  RecurPixel.Notify.Email.Mailgun/
  RecurPixel.Notify.Email.Resend/
  RecurPixel.Notify.Email.Postmark/
  RecurPixel.Notify.Email.AwsSes/
  RecurPixel.Notify.Sms.Twilio/
  RecurPixel.Notify.Sms.Vonage/
  RecurPixel.Notify.Sms.Plivo/
  RecurPixel.Notify.Sms.Sinch/
  RecurPixel.Notify.Sms.MessageBird/
  RecurPixel.Notify.Sms.AwsSns/
  RecurPixel.Notify.Push.Fcm/
  RecurPixel.Notify.Push.Apns/
  RecurPixel.Notify.Push.OneSignal/
  RecurPixel.Notify.Push.Expo/
  RecurPixel.Notify.WhatsApp.Twilio/
  RecurPixel.Notify.WhatsApp.MetaCloud/
  RecurPixel.Notify.WhatsApp.Vonage/
  RecurPixel.Notify.Slack/
  RecurPixel.Notify.Discord/
  RecurPixel.Notify.Teams/
  RecurPixel.Notify.Telegram/
  RecurPixel.Notify.Facebook/
  RecurPixel.Notify.Line/
  RecurPixel.Notify.Viber/
  RecurPixel.Notify.InApp/
tests/
  RecurPixel.Notify.Tests/
```

---

## Step 0 — Before You Touch Anything

```bash
# Verify all tests pass before starting
dotnet test
```

If any tests fail before you start, stop and report. Do not proceed with namespace changes on a broken baseline.

```bash
# Create a working branch
git checkout -b refactor/namespace-cleanup
```

---

## Step 1 — Audit Current Namespaces

Before changing anything, run this and save the output. We will verify against it at the end.

```bash
# Find all unique namespaces currently declared across the solution
grep -rh "^namespace " src/ tests/ | sort -u
```

```bash
# Find all unique using statements currently in the solution
grep -rh "^using RecurPixel" src/ tests/ | sort -u
```

Save both outputs. You will need them to verify nothing was missed.

---

## Step 2 — Target Namespace Map

Apply these namespace changes to every file in the solution. This is the complete mapping — do not invent new namespaces beyond what is listed here.

### RecurPixel.Notify.Core

| File Pattern | Current Namespace | New Namespace |
|---|---|---|
| `Models/NotificationPayload.cs` | `RecurPixel.Notify.Core.Models` | `RecurPixel.Notify` |
| `Models/NotifyResult.cs` | `RecurPixel.Notify.Core.Models` | `RecurPixel.Notify` |
| `Models/BulkNotifyResult.cs` | `RecurPixel.Notify.Core.Models` | `RecurPixel.Notify` |
| `Models/NotifyContext.cs` | `RecurPixel.Notify.Core.Models` | `RecurPixel.Notify` |
| `Models/NotifyUser.cs` | `RecurPixel.Notify.Core.Models` | `RecurPixel.Notify` |
| `Channels/INotificationChannel.cs` | `RecurPixel.Notify.Core.Channels` | `RecurPixel.Notify.Channels` |
| `Channels/NotificationChannelBase.cs` | `RecurPixel.Notify.Core.Channels` | `RecurPixel.Notify.Channels` |
| `Channels/ChannelAdapterAttribute.cs` | `RecurPixel.Notify.Core.Channels` | `RecurPixel.Notify.Channels` |
| `Options/NotifyOptions.cs` | `RecurPixel.Notify.Core.Options` | `RecurPixel.Notify` |
| `Options/RetryOptions.cs` | `RecurPixel.Notify.Core.Options` | `RecurPixel.Notify` |
| `Options/FallbackOptions.cs` | `RecurPixel.Notify.Core.Options` | `RecurPixel.Notify` |
| `Options/BulkOptions.cs` | `RecurPixel.Notify.Core.Options` | `RecurPixel.Notify` |
| `Options/Channels/EmailOptions.cs` | `RecurPixel.Notify.Core.Options.Channels` | `RecurPixel.Notify.Configuration` |
| `Options/Channels/SmsOptions.cs` | `RecurPixel.Notify.Core.Options.Channels` | `RecurPixel.Notify.Configuration` |
| `Options/Channels/PushOptions.cs` | `RecurPixel.Notify.Core.Options.Channels` | `RecurPixel.Notify.Configuration` |
| `Options/Channels/WhatsAppOptions.cs` | `RecurPixel.Notify.Core.Options.Channels` | `RecurPixel.Notify.Configuration` |
| `Options/Providers/*.cs` (all provider options) | `RecurPixel.Notify.Core.Options.Providers` | `RecurPixel.Notify.Configuration` |
| `Options/NamedProviderDefinition.cs` | `RecurPixel.Notify.Core.Options` | `RecurPixel.Notify.Configuration` |
| `Extensions/ServiceCollectionExtensions.cs` | `RecurPixel.Notify.Core.Extensions` | `RecurPixel.Notify` |
| `Validation/NotifyOptionsValidator.cs` | `RecurPixel.Notify.Core.Validation` | `RecurPixel.Notify` |

### RecurPixel.Notify.Orchestrator

| File Pattern | Current Namespace | New Namespace |
|---|---|---|
| `Services/INotifyService.cs` | `RecurPixel.Notify.Orchestrator.Services` | `RecurPixel.Notify` |
| `Services/NotifyService.cs` | `RecurPixel.Notify.Orchestrator.Services` | `RecurPixel.Notify` |
| `Extensions/ServiceCollectionExtensions.cs` | `RecurPixel.Notify.Orchestrator.Extensions` | `RecurPixel.Notify` |
| All other internal orchestrator types | `RecurPixel.Notify.Orchestrator.*` | `RecurPixel.Notify` |

### All Adapter Projects

Every adapter project follows the same rule: the public-facing class moves to the channel namespace, internal implementation types stay internal and keep whatever namespace they have.

| Type | Current Namespace | New Namespace |
|---|---|---|
| `SendGridChannel` (public sealed class) | `RecurPixel.Notify.Email.SendGrid` | `RecurPixel.Notify.Channels` |
| `SmtpChannel` (public sealed class) | `RecurPixel.Notify.Email.Smtp` | `RecurPixel.Notify.Channels` |
| `TwilioSmsChannel` (public sealed class) | `RecurPixel.Notify.Sms.Twilio` | `RecurPixel.Notify.Channels` |
| `FcmChannel` (public sealed class) | `RecurPixel.Notify.Push.Fcm` | `RecurPixel.Notify.Channels` |
| `SlackChannel` (public sealed class) | `RecurPixel.Notify.Slack` | `RecurPixel.Notify.Channels` |
| *(all other public channel classes)* | `RecurPixel.Notify.[Channel].[Provider]` | `RecurPixel.Notify.Channels` |
| `ServiceCollectionExtensions` in each adapter | `RecurPixel.Notify.[Channel].[Provider]` | `RecurPixel.Notify` |
| All internal implementation types | *(keep as-is)* | *(no change)* |

---

## Step 3 — Execution Order

Work through this in order. Run `dotnet build` after each project. Fix compiler errors before moving to the next project. Do not batch multiple projects and fix errors at the end.

```
1.  RecurPixel.Notify.Core              ← start here, everything depends on it
2.  RecurPixel.Notify.Orchestrator      ← depends on Core
3.  RecurPixel.Notify.Email.SendGrid    ← depends on Core only
4.  RecurPixel.Notify.Email.Smtp
5.  RecurPixel.Notify.Email.Mailgun
6.  RecurPixel.Notify.Email.Resend
7.  RecurPixel.Notify.Email.Postmark
8.  RecurPixel.Notify.Email.AwsSes
9.  RecurPixel.Notify.Sms.Twilio
10. RecurPixel.Notify.Sms.Vonage
11. RecurPixel.Notify.Sms.Plivo
12. RecurPixel.Notify.Sms.Sinch
13. RecurPixel.Notify.Sms.MessageBird
14. RecurPixel.Notify.Sms.AwsSns
15. RecurPixel.Notify.Push.Fcm
16. RecurPixel.Notify.Push.Apns
17. RecurPixel.Notify.Push.OneSignal
18. RecurPixel.Notify.Push.Expo
19. RecurPixel.Notify.WhatsApp.Twilio
20. RecurPixel.Notify.WhatsApp.MetaCloud
21. RecurPixel.Notify.WhatsApp.Vonage
22. RecurPixel.Notify.Slack
23. RecurPixel.Notify.Discord
24. RecurPixel.Notify.Teams
25. RecurPixel.Notify.Telegram
26. RecurPixel.Notify.Facebook
27. RecurPixel.Notify.Line
28. RecurPixel.Notify.Viber
29. RecurPixel.Notify.InApp
30. RecurPixel.Notify.Tests             ← last
```

---

## Step 4 — What to Change in Each File

For each file, make exactly two kinds of changes:

**Change 1 — The namespace declaration at the top of the file**

```csharp
// Before
namespace RecurPixel.Notify.Core.Models;

// After
namespace RecurPixel.Notify;
```

**Change 2 — Update using statements in every file that references the moved types**

After moving types to new namespaces, any file that previously imported the old namespace needs its using statement updated. Example:

```csharp
// Before
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Orchestrator.Services;

// After
using RecurPixel.Notify;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;
```

Remove any using statements that are now redundant because the file's own namespace matches.

---

## Step 5 — Rules to Follow During Execution

**Do not change:**
- Any logic, method bodies, or behaviour
- Internal class namespaces (only public types are being moved)
- File locations on disk — files stay where they are, only the namespace declaration changes
- `.csproj` project references — those are assembly references, not namespace references
- XML doc comments
- Test assertions

**Do not add:**
- New files
- New classes
- New extension methods
- Global using files

**If a type is `internal`:** leave its namespace exactly as-is. Internal types are not part of the public API and do not need to move.

**If you are unsure whether a type is meant to be public:** check if it has `public` access modifier. If yes, apply the mapping. If `internal`, skip it.

**If a file has both public and internal types:** split them only if necessary for the namespace to be correct. Prefer keeping related types in the same file — move the namespace of the whole file to match the public type, and if any internal types are in that file they will inherit the new namespace which is fine since they are internal.

---

## Step 6 — Verification After Each Project

After changing each project, run:

```bash
# Must produce zero errors
dotnet build src/RecurPixel.Notify.[ProjectName]/

# Check no old namespace strings remain in this project
grep -r "RecurPixel.Notify.Core.Models" src/RecurPixel.Notify.[ProjectName]/
grep -r "RecurPixel.Notify.Core.Channels" src/RecurPixel.Notify.[ProjectName]/
grep -r "RecurPixel.Notify.Core.Options" src/RecurPixel.Notify.[ProjectName]/
grep -r "RecurPixel.Notify.Orchestrator.Services" src/RecurPixel.Notify.[ProjectName]/
grep -r "RecurPixel.Notify.Orchestrator.Extensions" src/RecurPixel.Notify.[ProjectName]/
```

Each grep should return no results. If any old namespace strings remain, fix them before moving on.

---

## Step 7 — Final Verification

After all projects are done:

```bash
# Full solution build — must be zero errors, zero warnings about namespaces
dotnet build

# All tests must pass — same count as baseline, no new failures
dotnet test

# No old deep namespace patterns anywhere in the solution
grep -r "\.Core\.Models" src/
grep -r "\.Core\.Channels" src/
grep -r "\.Core\.Options" src/
grep -r "\.Orchestrator\.Services" src/
grep -r "\.Orchestrator\.Extensions" src/
```

All five greps must return zero results.

```bash
# Confirm the new namespaces exist as expected
grep -rh "^namespace " src/ | sort -u
```

The output should contain only these namespaces:
```
RecurPixel.Notify
RecurPixel.Notify.Channels
RecurPixel.Notify.Configuration
RecurPixel.Notify.Email.SendGrid      ← internal types only
RecurPixel.Notify.Email.Smtp          ← internal types only
RecurPixel.Notify.Sms.Twilio          ← internal types only
... (other adapter internal namespaces)
```

---

## Step 8 — Commit

```bash
git add -A
git commit -m "refactor: reorganize public namespaces for v0.2.0

Public types moved to clean namespaces:
- Application types → RecurPixel.Notify
- Channel contracts → RecurPixel.Notify.Channels  
- Configuration POCOs → RecurPixel.Notify.Configuration

No logic changes. All tests passing."
```

---

## What to Do If Something Goes Wrong

**Build errors after changing Core:** the most likely cause is a file in an adapter that still has the old `using RecurPixel.Notify.Core.Models` statement. Run the grep from Step 6 to find it.

**Test failures that were not failing before:** a namespace change should never cause a test failure unless the test was directly referencing a namespace string (e.g. in a reflection test). If tests fail, check if any test file has hardcoded namespace strings and update them.

**A type cannot be found after moving:** check if the file's namespace was updated but the using statement in the consuming file was not, or vice versa. Both must change together.

**Unsure about a specific file:** run `grep -n "namespace\|^using RecurPixel" path/to/file.cs` to see both the current namespace and its imports in one view before editing.

---

*RecurPixel.Notify — Namespace Cleanup Instructions for Claude Code. March 2026.*
