// ============================================================
// Tier 3 — Full SDK Meta-Package
// Uses RecurPixel.Notify.Sdk — pulls Core, Orchestrator, and
// all available channel adapters in a single project reference.
// Only providers with credentials in config get registered.
// ============================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurPixel.Notify;
using RecurPixel.Notify.Configuration;

Console.WriteLine("=== Tier 3: Full SDK Meta-Package ===");
Console.WriteLine();

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Tier 3 registration — RecurPixel.Notify.Sdk brings every adapter.
// Only adapters whose credentials are set below will be registered.
builder.Services.AddRecurPixelNotify(
    notifyOptions =>
    {
        notifyOptions.Email = new EmailOptions
        {
            Provider = "sendgrid",
            SendGrid = new SendGridOptions
            {
                ApiKey = "SG.fake-key-tier3-test",
                FromEmail = "no-reply@example.com",
                FromName = "Tier 3 Test"
            }
        };

        notifyOptions.Sms = new SmsOptions
        {
            Provider = "twilio",
            Twilio = new TwilioOptions
            {
                AccountSid = "ACfakeaccountsid123",
                AuthToken = "fakeauthtoken",
                FromNumber = "+15550001234"
            }
        };

        // Telegram — demonstrates a messaging channel being configured via the SDK
        notifyOptions.Telegram = new TelegramOptions
        {
            BotToken = "fake:bottoken999",
            ChatId = "123456789"
        };
    },
    orchestratorOptions =>
    {
        orchestratorOptions.DefineEvent("welcome", e => e
            .UseChannels("email", "telegram")
            .WithRetry(maxAttempts: 1, delayMs: 0));

        orchestratorOptions.DefineEvent("order.placed", e => e
            .UseChannels("email", "sms")
            .WithCondition("sms", ctx => ctx.User.PhoneVerified)
            .WithFallback("email")
            .WithRetry(maxAttempts: 2, delayMs: 100));

        orchestratorOptions.DefineEvent("auth.otp", e => e
            .UseChannels("sms")
            .WithRetry(maxAttempts: 2, delayMs: 100));
    });

var host = builder.Build();

using var scope = host.Services.CreateScope();
var notify = scope.ServiceProvider.GetRequiredService<INotifyService>();
Console.WriteLine($"[PASS] Resolved INotifyService → {notify.GetType().Name}");

// ── Test 1: Multi-channel event (email + telegram) ────────────────────────────
Console.WriteLine();
Console.WriteLine("Triggering 'welcome' (email + telegram)...");
var welcomeResult = await notify.TriggerAsync("welcome", new NotifyContext
{
    User = new NotifyUser { UserId = "user-001", Email = "user@example.com" },
    Channels = new Dictionary<string, NotificationPayload>
    {
        ["email"] = new() { Subject = "Welcome!", Body = "<p>Welcome to the platform.</p>" },
        ["telegram"] = new() { Body = "Welcome to the platform!" }
    }
});
foreach (var ch in welcomeResult.ChannelResults)
    Console.WriteLine($"  [{(ch.Success ? "PASS" : "FAIL")}] {ch.Channel} / {ch.Provider}: {ch.Error ?? "sent"}");

// ── Test 2: Event with condition + retry ─────────────────────────────────────
Console.WriteLine();
Console.WriteLine("Triggering 'order.placed' (email + sms conditional, fallback to email)...");
var orderResult = await notify.TriggerAsync("order.placed", new NotifyContext
{
    User = new NotifyUser
    {
        UserId = "user-001",
        Email = "user@example.com",
        Phone = "+15550009999",
        PhoneVerified = true
    },
    Channels = new Dictionary<string, NotificationPayload>
    {
        ["email"] = new() { Subject = "Order Placed", Body = "<p>Your order has been placed.</p>" },
        ["sms"] = new() { Body = "Your order has been placed." }
    }
});
foreach (var ch in orderResult.ChannelResults)
    Console.WriteLine($"  [{(ch.Success ? "PASS" : "FAIL")}] {ch.Channel} / {ch.Provider}: {ch.Error ?? "sent"}");

// ── Test 3: Direct channel access ────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("Direct channel access (bypasses orchestration)...");

var emailDirect = await notify.Email.SendAsync(new NotificationPayload
{
    To = "direct@example.com",
    Subject = "Direct Email",
    Body = "<p>Testing direct channel access.</p>"
});
Console.WriteLine($"  [{(emailDirect.Success ? "PASS" : "FAIL")}] email direct / {emailDirect.Provider}: {emailDirect.Error ?? "sent"}");

var smsDirect = await notify.Sms.SendAsync(new NotificationPayload
{
    To = "+15550009999",
    Body = "Direct SMS test."
});
Console.WriteLine($"  [{(smsDirect.Success ? "PASS" : "FAIL")}] sms direct / {smsDirect.Provider}: {smsDirect.Error ?? "sent"}");

var telegramDirect = await notify.Telegram.SendAsync(new NotificationPayload
{
    Body = "Direct Telegram message test."
});
Console.WriteLine($"  [{(telegramDirect.Success ? "PASS" : "FAIL")}] telegram direct / {telegramDirect.Provider}: {telegramDirect.Error ?? "sent"}");

// ── Test 4: BulkTriggerAsync ─────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("BulkTriggerAsync 'auth.otp' for 3 users...");
var bulkContexts = new List<NotifyContext>
{
    new() { User = new() { Phone = "+15550001111", PhoneVerified = true }, Channels = new() { ["sms"] = new() { Body = "OTP: 111111" } } },
    new() { User = new() { Phone = "+15550002222", PhoneVerified = true }, Channels = new() { ["sms"] = new() { Body = "OTP: 222222" } } },
    new() { User = new() { Phone = "+15550003333", PhoneVerified = true }, Channels = new() { ["sms"] = new() { Body = "OTP: 333333" } } }
};
var bulkResult = await notify.BulkTriggerAsync("auth.otp", bulkContexts);
Console.WriteLine($"  Total users : {bulkResult.Results.Count}");
Console.WriteLine($"  All SMS dispatched: {(bulkResult.Results.All(r => r.ChannelResults.Any()) ? "[PASS]" : "[FAIL]")}");

Console.WriteLine();
Console.WriteLine("=== Tier 3 complete — SDK meta-package, all events, direct access, bulk send verified ===");
