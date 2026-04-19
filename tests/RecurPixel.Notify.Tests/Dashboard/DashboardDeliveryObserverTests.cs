using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RecurPixel.Notify.Dashboard;

namespace RecurPixel.Notify.Tests.Dashboard;

public class DashboardDeliveryObserverTests
{
    private static (DashboardDeliveryObserver observer, Mock<INotificationLogStore> storeMock)
        BuildObserver(bool registerStore = true)
    {
        var storeMock = new Mock<INotificationLogStore>();
        storeMock
            .Setup(s => s.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        if (registerStore)
            services.AddScoped(_ => storeMock.Object);

        var sp = services.BuildServiceProvider();
        var observer = new DashboardDeliveryObserver(
            sp,
            NullLogger<DashboardDeliveryObserver>.Instance);

        return (observer, storeMock);
    }

    [Fact]
    public async Task OnDeliveryAsync_WritesLogToStore()
    {
        var (observer, storeMock) = BuildObserver();
        var result = new NotifyResult
        {
            Channel  = "email",
            Provider = "sendgrid",
            Success  = true,
            SentAt   = DateTime.UtcNow
        };

        await observer.OnDeliveryAsync(result);

        storeMock.Verify(
            s => s.AddAsync(It.Is<NotificationLog>(l =>
                l.Channel == "email" &&
                l.Provider == "sendgrid" &&
                l.Success == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDeliveryAsync_NoStoreRegistered_DoesNotThrow()
    {
        var (observer, _) = BuildObserver(registerStore: false);

        // Should complete silently — logs a debug warning instead of throwing
        await observer.OnDeliveryAsync(new NotifyResult { SentAt = DateTime.UtcNow });
    }

    [Fact]
    public async Task OnDeliveryAsync_BulkResult_IsBulkSetOnLog()
    {
        var (observer, storeMock) = BuildObserver();
        var result = new NotifyResult
        {
            Channel     = "sms",
            Provider    = "twilio",
            Success     = true,
            BulkBatchId = "batch-xyz",
            SentAt      = DateTime.UtcNow
        };

        await observer.OnDeliveryAsync(result);

        storeMock.Verify(
            s => s.AddAsync(
                It.Is<NotificationLog>(l => l.IsBulk && l.BulkBatchId == "batch-xyz"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDeliveryAsync_FailureResult_ErrorPreservedInLog()
    {
        var (observer, storeMock) = BuildObserver();
        var result = new NotifyResult
        {
            Channel  = "push",
            Provider = "fcm",
            Success  = false,
            Error    = "Invalid device token",
            SentAt   = DateTime.UtcNow
        };

        await observer.OnDeliveryAsync(result);

        storeMock.Verify(
            s => s.AddAsync(
                It.Is<NotificationLog>(l => !l.Success && l.Error == "Invalid device token"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
