// ============================================================
// Tier 1 — Direct Provider Usage
// No orchestrator. Registers individual adapters explicitly
// and resolves them as keyed INotificationChannel instances.
//
// v0.3.0 coverage: email:sendgrid + sms:msg91 (Phase 17)
// ============================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurPixel.Notify;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

Console.WriteLine("=== Tier 1: Direct Provider Usage ===");
Console.WriteLine();

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Register two independent providers — no orchestrator, each on a distinct channel key
builder.Services.AddSendGridChannel(new SendGridOptions
{
    ApiKey    = "SG.fake-key-tier1-test",
    FromEmail = "no-reply@example.com",
    FromName  = "Tier 1 Test"
});
builder.Services.AddMsg91SmsChannel(new Msg91SmsOptions
{
    AuthKey  = "fake-authkey-tier1-msg91",
    SenderId = "NOTIFY"
});

var host = builder.Build();
using var scope = host.Services.CreateScope();
var sp = scope.ServiceProvider;

// ── Test 1: email:sendgrid ─────────────────────────────────────────────────
var emailChannel = sp.GetRequiredKeyedService<INotificationChannel>("email:sendgrid");
Console.WriteLine($"[PASS] Resolved 'email:sendgrid' → {emailChannel.GetType().Name}");
Console.WriteLine("Attempting send (fake credentials — verifying runtime path)...");

var emailResult = await emailChannel.SendAsync(new NotificationPayload
{
    To      = "recipient@example.com",
    Subject = "Tier 1 Test Email",
    Body    = "<p>Hello from Tier 1 direct send.</p>"
});
PrintResult(emailResult);

// ── Test 2: sms:msg91 (Phase 17) ──────────────────────────────────────────
Console.WriteLine();
var smsChannel = sp.GetRequiredKeyedService<INotificationChannel>("sms:msg91");
Console.WriteLine($"[PASS] Resolved 'sms:msg91' → {smsChannel.GetType().Name}");
Console.WriteLine("Attempting send (fake credentials — verifying runtime path)...");

var smsResult = await smsChannel.SendAsync(new NotificationPayload
{
    To   = "+919999999999",
    Body = "Tier 1 MSG91 SMS test."
});
PrintResult(smsResult);

Console.WriteLine();
Console.WriteLine("=== Tier 1 complete — DI registration and channel resolution verified ===");

static void PrintResult(NotifyResult r)
{
    Console.WriteLine($"  Channel  : {r.Channel}");
    Console.WriteLine($"  Provider : {r.Provider}");
    Console.WriteLine($"  Success  : {r.Success}");
    if (!r.Success)
        Console.WriteLine($"  Error    : {r.Error}");
}
