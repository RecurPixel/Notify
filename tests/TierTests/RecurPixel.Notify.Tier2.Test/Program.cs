// ============================================================
// Tier 2 — Orchestrator + Selective Providers
// Uses RecurPixel.Notify (Core + Orchestrator meta-package).
// Provider adapters added individually alongside it.
// ============================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Channels;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Orchestrator.Extensions;
using RecurPixel.Notify.Orchestrator.Services;

Console.WriteLine("=== Tier 2: Orchestrator + Selective Providers ===");
Console.WriteLine();

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Tier 2 registration — RecurPixel.Notify meta-package handles Core + Orchestrator.
// Provider adapters are auto-discovered from loaded assemblies via AddRecurPixelNotify.
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
            Twilio = new TwilioOptions
            {
                AccountSid = "ACfakeaccountsid123",
                AuthToken  = "fakeauthtoken",
                FromNumber = "+15550001234"
            }
        };
    },
    orchestratorOptions =>
    {
        orchestratorOptions.DefineEvent("order.confirmed", e => e
            .UseChannels("email", "sms")
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

// ── Test 1: TriggerAsync with conditions ─────────────────────────────────────
Console.WriteLine();
Console.WriteLine("Triggering 'order.confirmed' (email + sms, sms gated on PhoneVerified)...");
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
        ["email"] = new() { Subject = "Order Confirmed", Body = "<p>Your order is confirmed.</p>" },
        ["sms"]   = new() { Body = "Your order is confirmed. #42" }
    }
};
var orderResult = await notify.TriggerAsync("order.confirmed", orderCtx);
Console.WriteLine($"  Event    : {orderResult.EventName}");
foreach (var ch in orderResult.ChannelResults)
    Console.WriteLine($"  [{(ch.Success ? "PASS" : "FAIL")}] {ch.Channel} / {ch.Provider}: {ch.Error ?? "sent"}");

// ── Test 2: TriggerAsync with condition blocking sms ─────────────────────────
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
        ["email"] = new() { Subject = "Order Confirmed", Body = "<p>Your order is confirmed.</p>" },
        ["sms"]   = new() { Body = "Your order is confirmed." }
    }
};
var noSmsResult = await notify.TriggerAsync("order.confirmed", noSmsCtx);
var smsSkipped = noSmsResult.ChannelResults.All(r => r.Channel != "sms");
Console.WriteLine($"  SMS skipped by condition : {(smsSkipped ? "[PASS]" : "[FAIL]")}");
foreach (var ch in noSmsResult.ChannelResults)
    Console.WriteLine($"  [{(ch.Success ? "PASS" : "FAIL")}] {ch.Channel} / {ch.Provider}: {ch.Error ?? "sent"}");

// ── Test 3: Direct channel access ────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("Direct channel access via notify.Email (bypasses event system)...");
var directResult = await notify.Email.SendAsync(new NotificationPayload
{
    To      = "direct@example.com",
    Subject = "Direct Send",
    Body    = "<p>Direct send bypasses orchestration.</p>"
});
Console.WriteLine($"  [{(directResult.Success ? "PASS" : "FAIL")}] email direct / {directResult.Provider}: {directResult.Error ?? "sent"}");

Console.WriteLine();
Console.WriteLine("=== Tier 2 complete — INotifyService, events, conditions, direct access verified ===");
