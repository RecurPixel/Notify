// ============================================================
// Tier 2 — Orchestrator + Selective Providers
// Uses AddRecurPixelNotify (Core + Orchestrator meta-package).
// Provider adapter assemblies loaded alongside for auto-discovery.
//
// v0.3.0 coverage: email:sendgrid + sms:twilio + whatsapp:msg91
// ============================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurPixel.Notify;
using RecurPixel.Notify.Configuration;

Console.WriteLine("=== Tier 2: Orchestrator + Selective Providers ===");
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
                ApiKey    = "SG.fake-key-tier2-test",
                FromEmail = "no-reply@example.com",
                FromName  = "Tier 2 Test"
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

        // Phase 17: MSG91 WhatsApp — demonstrates a third channel via auto-discovered registrar
        notifyOptions.WhatsApp = new WhatsAppOptions
        {
            Provider = "msg91",
            Msg91    = new Msg91WhatsAppOptions
            {
                AuthKey          = "fake-authkey-tier2-msg91",
                IntegratedNumber = "919876543210"
            }
        };
    },
    orchestratorOptions =>
    {
        orchestratorOptions.DefineEvent("order.confirmed", e => e
            .UseChannels("email", "sms", "whatsapp")
            .WithCondition("sms", ctx => ctx.User.PhoneVerified)
            .WithRetry(maxAttempts: 2, delayMs: 100));

        orchestratorOptions.DefineEvent("auth.otp", e => e
            .UseChannels("sms")
            .WithRetry(maxAttempts: 2, delayMs: 100));
    });

var host = builder.Build();
using var scope = host.Services.CreateScope();
var notify = scope.ServiceProvider.GetRequiredService<INotifyService>();
Console.WriteLine($"[PASS] Resolved INotifyService → {notify.GetType().Name}");

// ── Test 1: TriggerAsync — email + sms (gated) + whatsapp ────────────────
Console.WriteLine();
Console.WriteLine("Triggering 'order.confirmed' (email + sms + whatsapp, sms gated on PhoneVerified)...");
var orderCtx = new NotifyContext
{
    User = new NotifyUser
    {
        UserId        = "user-001",
        Email         = "customer@example.com",
        Phone         = "+15550009999",
        PhoneVerified = true
    },
    Channels = new Dictionary<string, NotificationPayload>
    {
        ["email"]    = new() { Subject = "Order Confirmed", Body = "<p>Your order is confirmed.</p>" },
        ["sms"]      = new() { Body = "Your order is confirmed. #42" },
        ["whatsapp"] = new() { To = "+919999999999", Body = "Your order is confirmed." }
    }
};
var orderResult = await notify.TriggerAsync("order.confirmed", orderCtx);
Console.WriteLine($"  Event    : {orderResult.EventName}");
foreach (var ch in orderResult.ChannelResults)
    Console.WriteLine($"  [{(ch.Success ? "PASS" : "FAIL")}] {ch.Channel} / {ch.Provider}: {ch.Error ?? "sent"}");

// ── Test 2: SMS skipped by condition — email + whatsapp proceed ───────────
Console.WriteLine();
Console.WriteLine("Triggering 'order.confirmed' with PhoneVerified=false (sms should be skipped)...");
var noSmsCtx = new NotifyContext
{
    User = new NotifyUser
    {
        UserId        = "user-002",
        Email         = "user2@example.com",
        Phone         = "+15550008888",
        PhoneVerified = false
    },
    Channels = new Dictionary<string, NotificationPayload>
    {
        ["email"]    = new() { Subject = "Order Confirmed", Body = "<p>Your order is confirmed.</p>" },
        ["sms"]      = new() { Body = "Your order is confirmed." },
        ["whatsapp"] = new() { To = "+919999999999", Body = "Your order is confirmed." }
    }
};
var noSmsResult = await notify.TriggerAsync("order.confirmed", noSmsCtx);
var smsSkipped  = noSmsResult.ChannelResults.All(r => r.Channel != "sms");
Console.WriteLine($"  SMS skipped by condition : {(smsSkipped ? "[PASS]" : "[FAIL]")}");
foreach (var ch in noSmsResult.ChannelResults)
    Console.WriteLine($"  [{(ch.Success ? "PASS" : "FAIL")}] {ch.Channel} / {ch.Provider}: {ch.Error ?? "sent"}");

// ── Test 3: Direct channel access ────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("Direct channel access (bypasses event system)...");

var emailDirect = await notify.Email.SendAsync(new NotificationPayload
{
    To      = "direct@example.com",
    Subject = "Direct Send",
    Body    = "<p>Direct send bypasses orchestration.</p>"
});
Console.WriteLine($"  [{(emailDirect.Success ? "PASS" : "FAIL")}] email direct / {emailDirect.Provider}: {emailDirect.Error ?? "sent"}");

var whatsAppDirect = await notify.WhatsApp.SendAsync(new NotificationPayload
{
    To   = "+919999999999",
    Body = "Direct WhatsApp message via MSG91."
});
Console.WriteLine($"  [{(whatsAppDirect.Success ? "PASS" : "FAIL")}] whatsapp direct / {whatsAppDirect.Provider}: {whatsAppDirect.Error ?? "sent"}");

Console.WriteLine();
Console.WriteLine("=== Tier 2 complete — INotifyService, events, conditions, MSG91 WhatsApp verified ===");
