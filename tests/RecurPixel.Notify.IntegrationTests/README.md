# RecurPixel.Notify — Integration Tests

Integration tests for all channel adapters. Every test hits a real provider API — no mocks.

---

## Setup

### 1. Create `appsettings.integration.json`

The file is git-ignored and must be created locally:

```bash
cp appsettings.integration.json.example appsettings.integration.json
```

Fill in the credentials for any adapters you want to test. Leave everything else blank — adapters with missing credentials are **automatically skipped**, no code changes needed.

### 2. Run

```bash
# Run all integration tests (skips anything not configured)
dotnet test --filter "Category=Integration"

# Run a specific adapter
dotnet test --filter "FullyQualifiedName~SendGrid"

# Run all email adapters
dotnet test --filter "FullyQualifiedName~Email"
```

### 3. CI / Environment Variables

You can inject credentials via environment variables instead of the JSON file.
Prefix every key with `NOTIFY_` and replace `:` with `__`:

```bash
NOTIFY_Notify__Email__SendGrid__ApiKey=SG.xxx
NOTIFY_Integration__ToEmail=test@example.com
```

---

## How It Works

One abstract base class — `ChannelIntegrationTest<TChannel>` — drives all tests.
Each adapter needs only a ~15-line concrete class implementing three methods:

| Method           | Purpose                                   |
| ---------------- | ----------------------------------------- |
| `IsConfigured()` | Returns true when credentials are present |
| `BuildChannel()` | Constructs the adapter from config        |
| `BuildPayload()` | Returns the test payload                  |

The base class runs three tests against every adapter:

| Test                                                | What It Checks                                                         |
| --------------------------------------------------- | ---------------------------------------------------------------------- |
| `SingleSend_ReturnsSuccess`                         | `SendAsync` returns `Success = true`, channel matches, `SentAt` is set |
| `BulkSend_AllSucceeded`                             | `SendBulkAsync` with 3 payloads — all succeed, total = 3               |
| `SingleSend_InvalidRecipient_ReturnsFalse_NotThrow` | Invalid recipient doesn't throw — adapter handles it gracefully        |

---

## Adapter Validation Status

Update this table as testing is completed. Commit the table, not the credentials.

| Channel  | Provider    | Unit Tests | Integration Tested | Community Confirmed |
| -------- | ----------- | ---------- | ------------------ | ------------------- |
| Email    | SendGrid    | ✅          | ✅ v1.0             | —                   |
| Email    | SMTP        | ✅          | ✅ v1.0             | —                   |
| Email    | Mailgun     | ✅          | —                  | —                   |
| Email    | Resend      | ✅          | —                  | —                   |
| Email    | Postmark    | ✅          | —                  | —                   |
| Email    | AWS SES     | ✅          | —                  | —                   |
| Email    | Azure Comm  | ✅          | —                  | —                   |
| SMS      | Twilio      | ✅          | ✅ v1.0             | —                   |
| SMS      | Vonage      | ✅          | —                  | —                   |
| SMS      | Plivo       | ✅          | —                  | —                   |
| SMS      | Sinch       | ✅          | —                  | —                   |
| SMS      | MessageBird | ✅          | —                  | —                   |
| SMS      | AWS SNS     | ✅          | —                  | —                   |
| SMS      | Azure Comm  | ✅          | —                  | —                   |
| Push     | FCM         | ✅          | —                  | —                   |
| Push     | APNs        | ✅          | —                  | —                   |
| Push     | OneSignal   | ✅          | —                  | —                   |
| Push     | Expo        | ✅          | —                  | —                   |
| WhatsApp | Twilio      | ✅          | ✅ v1.0             | —                   |
| WhatsApp | Meta Cloud  | ✅          | —                  | —                   |
| WhatsApp | Vonage      | ✅          | —                  | —                   |
| Slack    | —           | ✅          | ✅ v1.0             | —                   |
| Discord  | —           | ✅          | ✅ v1.0             | —                   |
| Teams    | —           | ✅          | —                  | —                   |
| Mattermost | —         | ✅          | —                  | —                   |
| Rocket.Chat | —        | ✅          | —                  | —                   |
| Telegram | —           | ✅          | ✅ v1.0             | —                   |
| Facebook | —           | ✅          | —                  | —                   |
| LINE     | —           | ✅          | —                  | —                   |
| Viber    | —           | ✅          | —                  | —                   |
| InApp    | —           | ✅          | ✅ v1.0             | —                   |

**Key:**
- ✅ = Confirmed
- ⏳ = Pending (adapter not yet built)
- — = Not yet tested

**Integration Tested** = RecurPixel team verified with real credentials, tag it with the version (e.g. `✅ v1.0`).
**Community Confirmed** = link to the GitHub issue or PR where a community member reported successful production use.

---

## Adding a New Adapter Test

1. Uncomment the relevant class in the appropriate file under `Email/`, `Sms/`, `Push/`, `WhatsApp/`, or `Messaging/`.
2. Ensure the `ProjectReference` in the `.csproj` is also uncommented.
3. Add credentials to your local `appsettings.integration.json`.
4. Run `dotnet test --filter "FullyQualifiedName~[AdapterName]"`.
5. Update the validation table in this README.

---

## .gitignore Entry

Ensure this is in your repo root `.gitignore`:

```
**/appsettings.integration.json
```
