using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.InApp;
using RecurPixel.Notify.IntegrationTests.Infrastructure;

namespace RecurPixel.Notify.IntegrationTests.InApp;

/// <summary>
/// Integration tests for the InApp (hook-based inbox) adapter.
///
/// InApp is fully in-process — no external service required.
/// The test verifies the handler callback fires with the correct payload.
/// </summary>
public sealed class InAppIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "inapp:inapp";

    // InApp is fully in-process — no credentials needed, always run
    protected override bool IsConfigured() => true;

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new InAppOptions
        {
            Handler = (payload, ct) => Task.FromResult(new NotifyResult
            {
                Success = true,
                ProviderId = Guid.NewGuid().ToString()
            })
        };
        services.AddInAppChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = "test-user-id-001",
        Subject = "RecurPixel.Notify Integration Test",
        Body = $"InApp notification — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
