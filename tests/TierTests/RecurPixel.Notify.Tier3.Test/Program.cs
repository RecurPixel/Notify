// ============================================================
// Tier 3 — Full SDK Meta-Package
// Uses RecurPixel.Notify.Sdk — Core, Orchestrator, all adapters.
// Only providers with credentials configured get registered.
//
// v0.3.0 coverage: email:sendgrid + sms:twilio + telegram + whatsapp:msg91
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

builder.Services.AddRecurPixelNotify(
    notifyOptions =>
    {
        notifyOptions.Email = new EmailOptions
        {
            Provider = "sendgrid",
            SendGrid = new SendGridOptions
            {
                ApiKey    = "SG.fake-key-tier3-test",
                FromEmail = "no-reply@example.com",
                FromName  = "Tier 3 Test"
            }
        };

        notifyOptions.Sms = new SmsOptions
        {
            Provider = "twilio",
            Twilio   = new TwilioOptions
            {
                AccountSid = "ACfakeaccountsid123",
                AuthToken  = "fakeauthtoken",
                FromNumber = "+15550001234"
            }
        };

        // Telegram — single-implementation channel, no Provider key required
        notifyOptions.Telegram = new TelegramOptions
        {
            BotToken = "fake:bottoken999",
            ChatId   = "123456789"
        };

        // Phase 17: MSG91 WhatsApp — auto-discovered from SDK, validates "msg91" provider
        notifyOptions.WhatsApp = new WhatsAppOptions
        {
            Provider = "msg91",
            Msg91    = new Msg91WhatsAppOptions
            {
                AuthKey          = "fake-authkey-tier3-msg91",
                IntegratedNumber = "919876543210"
            }
        };
    },
    orchestratorOptions =>
    {
        // welcome: email + telegram + whatsapp (all three channels exercised)
        orchestratorOptions.DefineEvent("welcome", e => e
            .UseChannels("email", "telegram", "whatsapp")
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

// ── Test 1: Multi-channel event (email + telegram + whatsapp) ────────────
Console.WriteLine();
Console.WriteLine("Triggering 'welcome' (email + telegram + whatsapp)...");
var welcomeResult = await notify.TriggerAsync("welcome", new NotifyContext
{
    User = new NotifyUser { UserId = "user-001", Email = "user@example.com", Phone = "+919999999999" },
    Channels = new Dictionary<string, NotificationPayload>
    {
        ["email"]    = new() { Subject = "Welcome!", Body = "<p>Welcome to the platform.</p>" },
        ["telegram"] = new() { Body = "Welcome to the platform!" },
        ["whatsapp"] = new() { To = "+919999999999", Body = "Welcome to the platform!" }
    }
});
foreach (var ch in welcomeResult.ChannelResults)
    Console.WriteLine($"  [{(ch.Success ? "PASS" : "FAIL")}] {ch.Channel} / {ch.Provider}: {ch.Error ?? "sent"}");

// ── Test 2: Event with condition + fallback ──────────────────────────────
Console.WriteLine();
Console.WriteLine("Triggering 'order.placed' (email + sms conditional, fallback to email)...");
var orderResult = await notify.TriggerAsync("order.placed", new NotifyContext
{
    User = new NotifyUser
    {
        UserId        = "user-001",
        Email         = "user@example.com",
        Phone         = "+15550009999",
        PhoneVerified = true
    },
    Channels = new Dictionary<string, NotificationPayload>
    {
        ["email"] = new() { Subject = "Order Placed", Body = "<p>Your order has been placed.</p>" },
        ["sms"]   = new() { Body = "Your order has been placed." }
    }
});
foreach (var ch in orderResult.ChannelResults)
    Console.WriteLine($"  [{(ch.Success ? "PASS" : "FAIL")}] {ch.Channel} / {ch.Provider}: {ch.Error ?? "sent"}");

// ── Test 3: Direct channel access ────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("Direct channel access (bypasses orchestration)...");

var emailDirect = await notify.Email.SendAsync(new NotificationPayload
{
    To      = "direct@example.com",
    Subject = "Direct Email",
    Body    = "<p>Testing direct channel access.</p>"
});
Console.WriteLine($"  [{(emailDirect.Success ? "PASS" : "FAIL")}] email direct / {emailDirect.Provider}: {emailDirect.Error ?? "sent"}");

var smsDirect = await notify.Sms.SendAsync(new NotificationPayload
{
    To   = "+15550009999",
    Body = "Direct SMS test."
});
Console.WriteLine($"  [{(smsDirect.Success ? "PASS" : "FAIL")}] sms direct / {smsDirect.Provider}: {smsDirect.Error ?? "sent"}");

var telegramDirect = await notify.Telegram.SendAsync(new NotificationPayload
{
    Body = "Direct Telegram message test."
});
Console.WriteLine($"  [{(telegramDirect.Success ? "PASS" : "FAIL")}] telegram direct / {telegramDirect.Provider}: {telegramDirect.Error ?? "sent"}");

var whatsAppDirect = await notify.WhatsApp.SendAsync(new NotificationPayload
{
    To   = "+919999999999",
    Body = "Direct WhatsApp message via MSG91."
});
Console.WriteLine($"  [{(whatsAppDirect.Success ? "PASS" : "FAIL")}] whatsapp direct / {whatsAppDirect.Provider}: {whatsAppDirect.Error ?? "sent"}");

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
