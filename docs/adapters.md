---
layout: default
title: Adapter Reference
nav_order: 6
---

# Adapter Reference

All available channel adapters, their configuration fields, native bulk support, and channel-specific `Metadata` keys.

---

## Email

### SendGrid

```bash
dotnet add package RecurPixel.Notify.Email.SendGrid
```

```json
"Email": {
  "Provider": "sendgrid",
  "SendGrid": {
    "ApiKey": "SG.xxxxxxxxxx",
    "FromEmail": "no-reply@yourapp.com",
    "FromName": "Your App"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `ApiKey` | ✅ | SendGrid API key (starts with `SG.`) |
| `FromEmail` | ✅ | Verified sender email address |
| `FromName` | ❌ | Display name for the sender |

**Payload fields used:** `To` (recipient email), `Subject`, `Body` (HTML supported).

**Native bulk:** ✅ Yes — up to 1,000 recipients per API call. `SendBulkAsync` uses the SendGrid batch endpoint automatically.

**Metadata keys:**

| Key | Description |
|---|---|
| `html` | Set to `"false"` to send plain text instead of HTML |
| `reply_to` | Reply-to email address |
| `categories` | Comma-separated SendGrid categories for analytics |

---

### SMTP

```bash
dotnet add package RecurPixel.Notify.Email.Smtp
```

```json
"Email": {
  "Provider": "smtp",
  "Smtp": {
    "Host": "smtp.office365.com",
    "Port": 587,
    "Username": "no-reply@yourapp.com",
    "Password": "your-password",
    "UseSsl": true,
    "FromEmail": "no-reply@yourapp.com",
    "FromName": "Your App"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `Host` | ✅ | SMTP server hostname |
| `Port` | ✅ | SMTP port (typically 587 or 465) |
| `Username` | ✅ | SMTP authentication username |
| `Password` | ✅ | SMTP authentication password |
| `UseSsl` | ✅ | Enable TLS/SSL |
| `FromEmail` | ✅ | Sender email address |
| `FromName` | ❌ | Sender display name |

**Native bulk:** ❌ No — base class loops single sends with a concurrency cap.

---

### Mailgun

```bash
dotnet add package RecurPixel.Notify.Email.Mailgun
```

```json
"Email": {
  "Provider": "mailgun",
  "Mailgun": {
    "ApiKey": "key-xxxxxxxxxx",
    "Domain": "mg.yourapp.com",
    "FromEmail": "no-reply@mg.yourapp.com",
    "FromName": "Your App",
    "Region": "us"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `ApiKey` | ✅ | Mailgun private API key |
| `Domain` | ✅ | Your verified Mailgun sending domain |
| `FromEmail` | ✅ | Sender email |
| `FromName` | ❌ | Sender display name |
| `Region` | ❌ | `"us"` (default) or `"eu"` |

**Native bulk:** ✅ Yes — uses Mailgun's recipient variables API.

---

### Resend

```bash
dotnet add package RecurPixel.Notify.Email.Resend
```

```json
"Email": {
  "Provider": "resend",
  "Resend": {
    "ApiKey": "re_xxxxxxxxxx",
    "FromEmail": "no-reply@yourapp.com",
    "FromName": "Your App"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `ApiKey` | ✅ | Resend API key |
| `FromEmail` | ✅ | Verified sender email |
| `FromName` | ❌ | Sender display name |

**Native bulk:** ❌ No — base class loops single sends.

---

### Postmark

```bash
dotnet add package RecurPixel.Notify.Email.Postmark
```

```json
"Email": {
  "Provider": "postmark",
  "Postmark": {
    "ApiKey": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "FromEmail": "no-reply@yourapp.com",
    "FromName": "Your App"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `ApiKey` | ✅ | Postmark server API token |
| `FromEmail` | ✅ | Verified sender email |
| `FromName` | ❌ | Sender display name |

**Native bulk:** ✅ Yes — uses Postmark's batch messages endpoint.

---

### AWS SES

```bash
dotnet add package RecurPixel.Notify.Email.AwsSes
```

```json
"Email": {
  "Provider": "awsses",
  "AwsSes": {
    "AccessKey": "AKIAIOSFODNN7EXAMPLE",
    "SecretKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "Region": "us-east-1",
    "FromEmail": "no-reply@yourapp.com",
    "FromName": "Your App"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `AccessKey` | ✅ | AWS access key ID |
| `SecretKey` | ✅ | AWS secret access key |
| `Region` | ✅ | AWS region (e.g. `us-east-1`) |
| `FromEmail` | ✅ | SES-verified sender email |
| `FromName` | ❌ | Sender display name |

**Native bulk:** ✅ Yes — uses the SES bulk email API.

---

### Azure Communication Services Email

```bash
dotnet add package RecurPixel.Notify.Email.AzureCommEmail
```

```json
"Email": {
  "Provider": "azurecommemail",
  "AzureCommEmail": {
    "ConnectionString": "endpoint=https://youracs.communication.azure.com/;accesskey=xxxxxxxxxx",
    "FromEmail": "DoNotReply@youracs.azurecomm.net",
    "FromName": "Your App"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `ConnectionString` | ✅ | Azure Communication Services connection string |
| `FromEmail` | ✅ | Sender email registered in ACS |
| `FromName` | ❌ | Sender display name |

**Native bulk:** ❌ No — base class loops single sends.

---

## SMS

### Twilio SMS

```bash
dotnet add package RecurPixel.Notify.Sms.Twilio
```

```json
"Sms": {
  "Provider": "twilio",
  "Twilio": {
    "AccountSid": "ACxxxxxxxxxx",
    "AuthToken": "xxxxxxxxxx",
    "FromNumber": "+15550001234"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `AccountSid` | ✅ | Twilio Account SID |
| `AuthToken` | ✅ | Twilio Auth Token |
| `FromNumber` | ✅ | Your Twilio phone number (E.164 format) |

**Native bulk:** ❌ No — base class loops single sends.

---

### Vonage (Nexmo) SMS

```bash
dotnet add package RecurPixel.Notify.Sms.Vonage
```

```json
"Sms": {
  "Provider": "vonage",
  "Vonage": {
    "ApiKey": "xxxxxxxxxx",
    "ApiSecret": "xxxxxxxxxx",
    "FromNumber": "YourApp"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `ApiKey` | ✅ | Vonage API key |
| `ApiSecret` | ✅ | Vonage API secret |
| `FromNumber` | ✅ | Sender ID or phone number |

**Native bulk:** ✅ Yes — uses Vonage bulk SMS API.

---

### Sinch

```bash
dotnet add package RecurPixel.Notify.Sms.Sinch
```

```json
"Sms": {
  "Provider": "sinch",
  "Sinch": {
    "ServicePlanId": "xxxxxxxxxx",
    "ApiToken": "xxxxxxxxxx",
    "FromNumber": "+15550001234"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `ServicePlanId` | ✅ | Sinch service plan ID |
| `ApiToken` | ✅ | Sinch API token |
| `FromNumber` | ✅ | Sinch virtual phone number |

**Native bulk:** ✅ Yes — uses Sinch batch SMS API.

---

### Plivo

```bash
dotnet add package RecurPixel.Notify.Sms.Plivo
```

```json
"Sms": {
  "Provider": "plivo",
  "Plivo": {
    "AuthId": "xxxxxxxxxx",
    "AuthToken": "xxxxxxxxxx",
    "FromNumber": "+15550001234"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `AuthId` | ✅ | Plivo Auth ID |
| `AuthToken` | ✅ | Plivo Auth Token |
| `FromNumber` | ✅ | Plivo phone number |

**Native bulk:** ❌ No — base class loops single sends.

---

### MessageBird

```bash
dotnet add package RecurPixel.Notify.Sms.MessageBird
```

```json
"Sms": {
  "Provider": "messagebird",
  "MessageBird": {
    "ApiKey": "xxxxxxxxxx",
    "FromNumber": "YourApp"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `ApiKey` | ✅ | MessageBird API key |
| `FromNumber` | ✅ | Sender name or phone number |

**Native bulk:** ❌ No — base class loops single sends.

---

### AWS SNS

```bash
dotnet add package RecurPixel.Notify.Sms.AwsSns
```

```json
"Sms": {
  "Provider": "awssns",
  "AwsSns": {
    "AccessKey": "AKIAIOSFODNN7EXAMPLE",
    "SecretKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "Region": "us-east-1",
    "SenderId": "YourApp"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `AccessKey` | ✅ | AWS access key ID |
| `SecretKey` | ✅ | AWS secret access key |
| `Region` | ✅ | AWS region |
| `SenderId` | ❌ | SMS sender ID (alphanumeric, supported regions only) |

**Native bulk:** ✅ Yes — publishes to SNS topic for fan-out.

---

### Azure Communication Services SMS

```bash
dotnet add package RecurPixel.Notify.Sms.AzureCommSms
```

```json
"Sms": {
  "Provider": "azurecommsms",
  "AzureCommSms": {
    "ConnectionString": "endpoint=https://youracs.communication.azure.com/;accesskey=xxxxxxxxxx",
    "FromNumber": "+18331234567"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `ConnectionString` | ✅ | Azure Communication Services connection string |
| `FromNumber` | ✅ | Phone number purchased from ACS (E.164 format) |

**Native bulk:** ✅ Yes — uses ACS SMS batch API, chunks of 100 recipients per call.

---

## Push Notifications

### FCM (Firebase Cloud Messaging)

```bash
dotnet add package RecurPixel.Notify.Push.Fcm
```

```json
"Push": {
  "Provider": "fcm",
  "Fcm": {
    "ProjectId": "your-firebase-project",
    "ServiceAccountJson": "{ \"type\": \"service_account\", ... }"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `ProjectId` | ✅ | Firebase project ID |
| `ServiceAccountJson` | ✅ | Full service account JSON (as a string) |

**Native bulk:** ✅ Yes — uses FCM multicast, up to 500 device tokens per call.

**Payload fields:** `To` = device registration token. `Subject` = notification title. `Body` = notification body.

**Metadata keys:**

| Key | Description |
|---|---|
| `image_url` | URL of image to show in the notification |
| `click_action` | Android intent / iOS category |
| `data` | JSON string of custom key-value data payload |

---

### APNs (Apple Push Notification Service)

```bash
dotnet add package RecurPixel.Notify.Push.Apns
```

```json
"Push": {
  "Provider": "apns",
  "Apns": {
    "KeyId": "XXXXXXXXXX",
    "TeamId": "XXXXXXXXXX",
    "BundleId": "com.yourapp.app",
    "PrivateKey": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `KeyId` | ✅ | APNs auth key ID (from Apple Developer portal) |
| `TeamId` | ✅ | Apple Developer Team ID |
| `BundleId` | ✅ | App bundle identifier |
| `PrivateKey` | ✅ | Contents of the `.p8` private key file |

**Native bulk:** ❌ No — APNs is one notification per HTTP/2 request. Base class loops.

**Metadata keys:**

| Key | Description |
|---|---|
| `badge` | Badge count to display on app icon |
| `sound` | Sound name, or `"default"` |
| `content_available` | `"1"` for silent/background push |
| `mutable_content` | `"1"` to allow notification service extension |
| `category` | APNs category identifier |

---

### OneSignal

```bash
dotnet add package RecurPixel.Notify.Push.OneSignal
```

```json
"Push": {
  "Provider": "onesignal",
  "OneSignal": {
    "AppId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ApiKey": "xxxxxxxxxx"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `AppId` | ✅ | OneSignal App ID |
| `ApiKey` | ✅ | OneSignal REST API key |

**Native bulk:** ✅ Yes — uses OneSignal's bulk notifications API.

---

### Expo Push

```bash
dotnet add package RecurPixel.Notify.Push.Expo
```

```json
"Push": {
  "Provider": "expo",
  "Expo": {
    "AccessToken": "xxxxxxxxxx"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `AccessToken` | ❌ | Expo access token (required for production apps) |

**Native bulk:** ✅ Yes — uses Expo's push ticket batch API.

---

## WhatsApp

### Twilio WhatsApp

```bash
dotnet add package RecurPixel.Notify.WhatsApp.Twilio
```

```json
"WhatsApp": {
  "Provider": "twilio",
  "Twilio": {
    "AccountSid": "ACxxxxxxxxxx",
    "AuthToken": "xxxxxxxxxx",
    "FromNumber": "whatsapp:+14155238886"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `AccountSid` | ✅ | Twilio Account SID |
| `AuthToken` | ✅ | Twilio Auth Token |
| `FromNumber` | ✅ | WhatsApp-enabled number with `whatsapp:` prefix |

**Payload:** `To` must include the `whatsapp:` prefix (e.g. `whatsapp:+919876543210`).

**Native bulk:** ❌ No — Meta messaging policy prohibits bulk unsolicited WhatsApp. Base class loops with rate cap.

---

### Meta Cloud API

```bash
dotnet add package RecurPixel.Notify.WhatsApp.MetaCloud
```

```json
"WhatsApp": {
  "Provider": "metacloud",
  "MetaCloud": {
    "PhoneNumberId": "1234567890",
    "AccessToken": "EAAxxxxxxxxxxxxx",
    "ApiVersion": "v18.0"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `PhoneNumberId` | ✅ | Meta Business phone number ID |
| `AccessToken` | ✅ | Permanent system user access token |
| `ApiVersion` | ❌ | API version (default: `v18.0`) |

**Native bulk:** ❌ No — Meta policy restricts bulk WhatsApp sends. Base class loops.

**Metadata keys:**

| Key | Description |
|---|---|
| `template_name` | Name of an approved WhatsApp message template |
| `template_language` | Language code for the template (e.g. `en_US`) |

---

### Vonage WhatsApp

```bash
dotnet add package RecurPixel.Notify.WhatsApp.Vonage
```

```json
"WhatsApp": {
  "Provider": "vonage",
  "Vonage": {
    "ApiKey": "xxxxxxxxxx",
    "ApiSecret": "xxxxxxxxxx",
    "FromNumber": "+14155550100"
  }
}
```

| Field | Required | Description |
|---|---|---|
| `ApiKey` | ✅ | Vonage API key |
| `ApiSecret` | ✅ | Vonage API secret |
| `FromNumber` | ✅ | WhatsApp-enabled number registered with Vonage |

**Payload fields:** `To` = recipient phone number. `Subject` and `Body` are combined as the message text.

**Native bulk:** ❌ No — Meta policy restricts bulk WhatsApp sends. Base class loops.

---

## Team Collaboration

### Slack

```bash
dotnet add package RecurPixel.Notify.Slack
```

```json
"Slack": {
  "WebhookUrl": "https://hooks.slack.com/services/T00000000/B00000000/XXXXXXXXXXXXXXXX",
  "BotToken": "xoxb-xxxxxxxxxx"
}
```

| Field | Required | Description |
|---|---|---|
| `WebhookUrl` | ✅* | Incoming webhook URL |
| `BotToken` | ✅* | Bot token for Bot API (alternative to webhook) |

*One of `WebhookUrl` or `BotToken` is required.

**Payload fields:** `Body` = message text. `Subject` = optional header block.

**Metadata keys:**

| Key | Description |
|---|---|
| `channel` | Override the target channel (Bot API only) |
| `icon_emoji` | Emoji to use as the bot avatar (e.g. `:robot_face:`) |
| `username` | Override the bot display name |
| `blocks` | JSON string of Slack Block Kit blocks |

**Native bulk:** ❌ No — one message per webhook call. Base class loops.

---

### Discord

```bash
dotnet add package RecurPixel.Notify.Discord
```

```json
"Discord": {
  "WebhookUrl": "https://discord.com/api/webhooks/000000000000000000/xxxxxxxxxxxx"
}
```

| Field | Required | Description |
|---|---|---|
| `WebhookUrl` | ✅ | Discord webhook URL |

**Payload fields:** `Body` = message content. `Subject` = optional embed title.

**Metadata keys:**

| Key | Description |
|---|---|
| `username` | Override the webhook display name |
| `avatar_url` | Override the webhook avatar image URL |
| `tts` | `"true"` to send as text-to-speech |
| `embeds` | JSON string of Discord embed objects |

**Native bulk:** ❌ No — one message per webhook call.

---

### Microsoft Teams

```bash
dotnet add package RecurPixel.Notify.Teams
```

```json
"Teams": {
  "WebhookUrl": "https://outlook.office.com/webhook/xxxxxxxx@xxxxxxxx/IncomingWebhook/xxxxxxxxxx/xxxxxxxxxx"
}
```

| Field | Required | Description |
|---|---|---|
| `WebhookUrl` | ✅ | Incoming webhook connector URL |

**Payload fields:** `Subject` = card title. `Body` = card text content.

**Metadata keys:**

| Key | Description |
|---|---|
| `theme_color` | Hex colour for the card accent bar (e.g. `0076D7`) |
| `card_json` | Full Adaptive Card JSON string (overrides Subject/Body) |

**Native bulk:** ❌ No — one message per webhook call.

---

### Mattermost

```bash
dotnet add package RecurPixel.Notify.Mattermost
```

```json
"Mattermost": {
  "WebhookUrl": "https://mattermost.yourapp.com/hooks/xxxxxxxxxx",
  "Username": "NotifyBot",
  "Channel": "town-square"
}
```

| Field | Required | Description |
|---|---|---|
| `WebhookUrl` | ✅ | Incoming webhook URL from Mattermost integrations |
| `Username` | ❌ | Override the bot display name |
| `Channel` | ❌ | Override the target channel (e.g. `town-square`) |

**Payload fields:** `Subject` = bold header. `Body` = message text. Formatted as Markdown.

**Native bulk:** ❌ No — one message per webhook call.

---

### Rocket.Chat

```bash
dotnet add package RecurPixel.Notify.RocketChat
```

```json
"RocketChat": {
  "WebhookUrl": "https://rocketchat.yourapp.com/hooks/xxxxxxxxxx",
  "Username": "NotifyBot",
  "Channel": "#general"
}
```

| Field | Required | Description |
|---|---|---|
| `WebhookUrl` | ✅ | Incoming webhook URL from Rocket.Chat administration |
| `Username` | ❌ | Override the bot display name |
| `Channel` | ❌ | Override the target channel (e.g. `#general`) |

**Payload fields:** `Subject` = bold header. `Body` = message text. Formatted as Markdown.

**Native bulk:** ❌ No — one message per webhook call.

---

## Social & Messaging

### Telegram

```bash
dotnet add package RecurPixel.Notify.Telegram
```

```json
"Telegram": {
  "BotToken": "123456789:ABCdefGHIjklMNOpqrsTUVwxyz",
  "ChatId": "-1001234567890"
}
```

| Field | Required | Description |
|---|---|---|
| `BotToken` | ✅ | Telegram Bot API token from @BotFather |
| `ChatId` | ✅ | Default target chat/group/channel ID |

**Payload fields:** `Body` = message text. HTML and Markdown supported via Metadata.

**Metadata keys:**

| Key | Description |
|---|---|
| `chat_id` | Override the default `ChatId` for this message |
| `parse_mode` | `"HTML"` or `"MarkdownV2"` |
| `disable_notification` | `"true"` to send silently |
| `reply_to_message_id` | Message ID to reply to |

**Native bulk:** ❌ No — per-user DMs loop. Channel broadcasts use a single `chat_id`.

---

### Facebook Messenger

```bash
dotnet add package RecurPixel.Notify.Facebook
```

```json
"Facebook": {
  "PageAccessToken": "EAAxxxxxxxxxxxxx",
  "ApiVersion": "v18.0"
}
```

| Field | Required | Description |
|---|---|---|
| `PageAccessToken` | ✅ | Facebook Page access token |
| `ApiVersion` | ❌ | Graph API version (default: `v18.0`) |

**Payload fields:** `To` = recipient's Facebook page-scoped user ID (PSID). `Body` = message text.

**Native bulk:** ❌ No — Messenger API is per-user. Base class loops.

---

### LINE

```bash
dotnet add package RecurPixel.Notify.Line
```

```json
"Line": {
  "ChannelAccessToken": "xxxxxxxxxx",
  "ChannelSecret": "xxxxxxxxxx"
}
```

| Field | Required | Description |
|---|---|---|
| `ChannelAccessToken` | ✅ | LINE Messaging API channel access token |
| `ChannelSecret` | ✅ | LINE channel secret |

**Payload fields:** `To` = LINE user ID. `Body` = message text.

**Native bulk:** ❌ No — per-user messages loop. Use LINE broadcast endpoint for channel-wide pushes via Metadata.

---

### Viber

```bash
dotnet add package RecurPixel.Notify.Viber
```

```json
"Viber": {
  "AuthToken": "xxxxxxxxxx",
  "SenderName": "Your App",
  "SenderAvatar": "https://yourapp.com/avatar.png"
}
```

| Field | Required | Description |
|---|---|---|
| `AuthToken` | ✅ | Viber service auth token |
| `SenderName` | ✅ | Sender display name (max 28 characters) |
| `SenderAvatar` | ❌ | URL to sender avatar image |

**Payload fields:** `To` = Viber user ID. `Body` = message text.

**Native bulk:** ❌ No — base class loops.

---

## In-App

### InApp (Hook-Based)

```bash
dotnet add package RecurPixel.Notify.InApp
```

The InApp channel does not call an external API. It calls your hook with the payload — you define what "in-app delivery" means for your system.

```json
"InApp": {
  "Enabled": true
}
```

```csharp
options.InApp.OnDeliver(async payload =>
{
    await db.InAppNotifications.AddAsync(new InAppNotification
    {
        UserId    = payload.Metadata.GetValueOrDefault("user_id")?.ToString(),
        Title     = payload.Subject,
        Body      = payload.Body,
        CreatedAt = DateTime.UtcNow,
        IsRead    = false
    });
    await db.SaveChangesAsync();
});
```

You own the schema, the table, and the read/unread logic. We call your hook.

---

## Native Bulk Support Summary

| Channel | Provider | Native Bulk | Limit |
|---|---|---|---|
| Email | SendGrid | ✅ | 1,000/call |
| Email | AwsSes | ✅ | Batch API |
| Email | Postmark | ✅ | Batch endpoint |
| Email | Mailgun | ✅ | Recipient variables |
| Email | AzureCommEmail | ❌ | Loop |
| Email | Resend | ❌ | Loop |
| Email | SMTP | ❌ | Loop |
| SMS | Twilio | ❌ | Loop |
| SMS | Vonage | ✅ | Bulk SMS API |
| SMS | AwsSns | ✅ | Topic publish |
| SMS | AzureCommSms | ✅ | 100/call |
| SMS | Sinch | ✅ | Batch SMS API |
| SMS | Plivo | ❌ | Loop |
| SMS | MessageBird | ❌ | Loop |
| Push | FCM | ✅ | 500 tokens/call |
| Push | APNs | ❌ | Loop |
| Push | OneSignal | ✅ | Bulk API |
| Push | Expo | ✅ | Batch tickets |
| WhatsApp | Any | ❌ | Loop (Meta policy) |
| Slack | — | ❌ | Loop |
| Discord | — | ❌ | Loop |
| Teams | — | ❌ | Loop |
| Mattermost | — | ❌ | Loop |
| Rocket.Chat | — | ❌ | Loop |
| Telegram | — | ❌ | Loop (DMs) |
| Facebook | — | ❌ | Loop |
| LINE | — | ❌ | Loop |
| Viber | — | ❌ | Loop |

Adapters without native bulk use the base class loop with a configurable concurrency cap (default: 10 concurrent). Configure via `BulkOptions.ConcurrencyLimit` in `appsettings.json`.
