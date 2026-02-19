using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using Xunit;

namespace RecurPixel.Notify.IntegrationTests.Infrastructure;

/// <summary>
/// Generic base class for all channel integration tests.
///
/// Subclasses must implement:
///   - IsConfigured()     → return true when credentials are present in appsettings.integration.json
///   - ServiceKey         → the DI keyed service key, e.g. "email:sendgrid"
///   - RegisterServices() → call the adapter's AddXxx() extension on IServiceCollection
///   - BuildPayload()     → return a valid NotificationPayload for this channel
///
/// Two tests run automatically for every adapter:
///   1. SingleSend_ReturnsSuccess  — calls SendAsync with one payload
///   2. BulkSend_AllSucceeded      — calls SendBulkAsync with three payloads
///
/// Both tests skip automatically when IsConfigured() returns false.
/// No test ever fails because of missing credentials — it skips cleanly.
/// </summary>
public abstract class ChannelIntegrationTest
{
    protected IConfiguration Config => TestConfiguration.Instance;

    // ── Abstract members ──────────────────────────────────────────────

    /// <summary>
    /// Return true when all required credentials are present in appsettings.integration.json.
    /// When false, both tests skip with a clear message — no failure is recorded.
    /// </summary>
    protected abstract bool IsConfigured();

    /// <summary>
    /// The DI keyed service key for this adapter, e.g. "email:sendgrid", "sms:twilio".
    /// Must match the key used in the adapter's AddXxx() DI registration.
    /// </summary>
    protected abstract string ServiceKey { get; }

    /// <summary>
    /// Register adapter services into the provided ServiceCollection.
    /// Read credentials from <paramref name="config"/> and call the adapter's extension method.
    /// Phase 10/11/12 stubs throw InvalidOperationException here until the adapter is built.
    /// </summary>
    protected abstract void RegisterServices(IServiceCollection services, IConfiguration config);

    /// <summary>
    /// Build a valid NotificationPayload for this channel.
    /// Use recipient addresses from Config["Integration:ToXxx"].
    /// </summary>
    protected abstract NotificationPayload BuildPayload(IConfiguration config);

    // ── Internal helpers ──────────────────────────────────────────────

    private INotificationChannel ResolveChannel()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddLogging();
        RegisterServices(services, Config);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredKeyedService<INotificationChannel>(ServiceKey);
    }

    // ── Tests ─────────────────────────────────────────────────────────

    [SkippableFact]
    public async Task SingleSend_ReturnsSuccess()
    {
        Skip.IfNot(IsConfigured(),
            $"[{ServiceKey}] Credentials not configured — skipping. " +
            $"Fill in appsettings.integration.json to enable this test.");

        var channel = ResolveChannel();
        var payload = BuildPayload(Config);

        var result = await channel.SendAsync(payload);

        Assert.True(result.Success,
            $"[{ServiceKey}] SendAsync failed. Error: {result.Error}");
        Assert.Equal(ServiceKey.Split(':')[0], result.Channel);
        Assert.True(result.SentAt > DateTime.MinValue,
            "SentAt must be set on a successful send.");
    }

    [SkippableFact]
    public async Task BulkSend_AllSucceeded()
    {
        Skip.IfNot(IsConfigured(),
            $"[{ServiceKey}] Credentials not configured — skipping bulk test.");

        var channel = ResolveChannel();

        var payloads = Enumerable.Range(1, 3)
            .Select(i =>
            {
                var p = BuildPayload(Config);
                // Tag each message so they are distinguishable in the inbox / console
                p.Subject = $"[Bulk #{i}] {p.Subject}";
                return p;
            })
            .ToList<NotificationPayload>();

        var result = await channel.SendBulkAsync(payloads);

        Assert.Equal(3, result.Total);
        Assert.True(result.AllSucceeded,
            $"[{ServiceKey}] BulkSend had {result.FailureCount} failure(s):\n" +
            string.Join("\n", result.Failures.Select(f =>
                $"  Recipient={f.Recipient}  Error={f.Error}")));
        Assert.Equal(ServiceKey.Split(':')[0], result.Channel);
    }
}
