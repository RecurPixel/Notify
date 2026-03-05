// ============================================================
// Tier 1 — Direct Provider Usage
// No orchestrator. One channel, one provider, minimal footprint.
// Resolves INotificationChannel keyed as "email:sendgrid".
// ============================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Email.SendGrid;

Console.WriteLine("=== Tier 1: Direct Provider Usage ===");
Console.WriteLine();

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Tier 1 registration — explicit, no orchestrator
builder.Services.AddSendGridChannel(new SendGridOptions
{
    ApiKey    = "SG.fake-key-tier1-test",
    FromEmail = "no-reply@example.com",
    FromName  = "Tier 1 Test"
});

var host = builder.Build();

using var scope = host.Services.CreateScope();
var sp = scope.ServiceProvider;

// Resolve the keyed channel — "email:sendgrid" is the registration key
var channel = sp.GetRequiredKeyedService<INotificationChannel>("email:sendgrid");
Console.WriteLine($"[PASS] Resolved INotificationChannel 'email:sendgrid' → {channel.GetType().Name}");

// Send test — credentials are fake so the call will fail, but the runtime path executes
Console.WriteLine();
Console.WriteLine("Attempting send (fake credentials — verifying runtime path)...");
var result = await channel.SendAsync(new NotificationPayload
{
    To      = "recipient@example.com",
    Subject = "Tier 1 Test Email",
    Body    = "<p>Hello from Tier 1 direct send.</p>"
});

Console.WriteLine($"  Channel  : {result.Channel}");
Console.WriteLine($"  Provider : {result.Provider}");
Console.WriteLine($"  Success  : {result.Success}");
if (!result.Success)
    Console.WriteLine($"  Error    : {result.Error}");

Console.WriteLine();
Console.WriteLine("=== Tier 1 complete — DI registration and channel resolution verified ===");
