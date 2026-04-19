using Microsoft.Extensions.DependencyInjection;

namespace RecurPixel.Notify.Tests.Dashboard;

public class DashboardRegistrationTests
{
    [Fact]
    public void AddNotifyDashboard_RegistersObserverAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotifyDashboard();

        var provider = services.BuildServiceProvider();
        var observer = provider.GetService<INotifyDeliveryObserver>();

        Assert.NotNull(observer);
    }

    [Fact]
    public void AddNotifyDashboard_RegistersDashboardOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotifyDashboard(o =>
        {
            o.RoutePrefix = "my-dashboard";
            o.PageSize    = 100;
            o.RequireRole = "Admin";
        });

        var provider = services.BuildServiceProvider();
        var options  = provider.GetRequiredService<DashboardOptions>();

        Assert.Equal("my-dashboard", options.RoutePrefix);
        Assert.Equal(100,            options.PageSize);
        Assert.Equal("Admin",        options.RequireRole);
    }

    [Fact]
    public void AddNotifyDashboard_DefaultOptions_UsedWhenNoConfigureAction()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotifyDashboard();

        var options = services.BuildServiceProvider().GetRequiredService<DashboardOptions>();

        Assert.Equal("notify-dashboard", options.RoutePrefix);
        Assert.Equal("Notifications",   options.PageTitle);
        Assert.Equal(50,                options.PageSize);
        Assert.Null(options.RequireRole);
        Assert.Null(options.PolicyName);
    }

    [Fact]
    public void AddNotifyDashboard_CalledTwice_DoesNotRegisterDuplicateOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotifyDashboard(o => o.RoutePrefix = "first");
        services.AddNotifyDashboard(o => o.RoutePrefix = "second");

        var provider = services.BuildServiceProvider();
        // TryAddSingleton: first registration wins
        var options = provider.GetRequiredService<DashboardOptions>();
        Assert.Equal("first", options.RoutePrefix);
    }

    [Fact]
    public async Task AddNotifyDashboard_ObserverInvokedByOrchestrator()
    {
        var deliveredToObserver = new List<NotifyResult>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(new NotifyOptions
        {
            Email = new EmailOptions { Provider = "sendgrid" }
        }));

        var emailMock = new Moq.Mock<INotificationChannel>();
        emailMock.Setup(m => m.ChannelName).Returns("email");
        emailMock.Setup(m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotifyResult { Success = true, Channel = "email", SentAt = DateTime.UtcNow });

        services.AddKeyedSingleton<INotificationChannel>("email:sendgrid", (_, _) => emailMock.Object);

        // Register a test observer that captures results
        services.AddSingleton<INotifyDeliveryObserver>(new TestObserver(deliveredToObserver));

        services.AddRecurPixelNotifyOrchestrator(o =>
            o.DefineEvent("test.event", e => e.UseChannels("email")));

        var svc = services.BuildServiceProvider().GetRequiredService<INotifyService>();

        var ctx = new NotifyContext
        {
            User     = new NotifyUser { UserId = "u1", Email = "a@b.com" },
            Channels = new Dictionary<string, NotificationPayload>
            {
                ["email"] = new() { To = "a@b.com", Subject = "Test", Body = "Body" }
            }
        };

        await svc.TriggerAsync("test.event", ctx);

        Assert.Single(deliveredToObserver);
        Assert.Equal("email", deliveredToObserver[0].Channel);
    }

    private sealed class TestObserver : INotifyDeliveryObserver
    {
        private readonly List<NotifyResult> _results;
        public TestObserver(List<NotifyResult> results) => _results = results;
        public Task OnDeliveryAsync(NotifyResult result, CancellationToken ct = default)
        {
            _results.Add(result);
            return Task.CompletedTask;
        }
    }
}
