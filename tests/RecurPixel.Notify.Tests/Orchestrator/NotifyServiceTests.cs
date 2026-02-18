using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options.Channels;
using RecurPixel.Notify.Orchestrator.Extensions;
using RecurPixel.Notify.Orchestrator.Options;
using RecurPixel.Notify.Orchestrator.Services;

namespace RecurPixel.Notify.Tests.Orchestrator;

public class NotifyServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (INotifyService service, List<NotifyResult> hookResults) BuildService(
        Action<OrchestratorOptions> configureOrch,
        Action<NotifyOptions> configureNotify,
        Mock<INotificationChannel>? emailMock = null,
        Mock<INotificationChannel>? smsMock = null)
    {
        var hookResults = new List<NotifyResult>();

        var notifyOptions = new NotifyOptions
        {
            Email = new EmailOptions { Provider = "sendgrid" },
            Sms = new SmsOptions { Provider = "twilio" }
        };
        configureNotify(notifyOptions);

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(notifyOptions));
        services.AddLogging();

        if (emailMock is not null)
            services.AddKeyedSingleton<INotificationChannel>("email:sendgrid", (_, _) => emailMock.Object);

        if (smsMock is not null)
            services.AddKeyedSingleton<INotificationChannel>("sms:twilio", (_, _) => smsMock.Object);

        services.AddRecurPixelNotifyOrchestrator(o =>
        {
            o.OnDelivery(r => { hookResults.Add(r); return Task.CompletedTask; });
            configureOrch(o);
        });

        var svc = services.BuildServiceProvider().GetRequiredService<INotifyService>();
        return (svc, hookResults);
    }

    private static Mock<INotificationChannel> MakeMock(string channel, bool success = true, string? error = null)
    {
        var mock = new Mock<INotificationChannel>();
        mock.Setup(m => m.ChannelName).Returns(channel);
        mock.Setup(m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotifyResult
            {
                Success = success,
                Channel = channel,
                Provider = channel,
                SentAt = DateTime.UtcNow,
                Error = error
            });
        return mock;
    }

    private static NotifyContext MakeContext() => new()
    {
        User = new NotifyUser { UserId = "u1", Email = "a@b.com", Phone = "+1234567890" },
        Channels = new Dictionary<string, NotificationPayload>
        {
            ["email"] = new() { To = "a@b.com", Subject = "Hello", Body = "Body" },
            ["sms"] = new() { To = "+1234567890", Body = "SMS body" }
        }
    };

    // ── TriggerAsync — basic dispatch ─────────────────────────────────────────

    [Fact]
    public async Task TriggerAsync_SingleChannel_ReturnsSuccess()
    {
        var emailMock = MakeMock("email");
        var (svc, _) = BuildService(
            o => o.DefineEvent("order.placed", e => e.UseChannels("email")),
            n => { },
            emailMock: emailMock);

        var result = await svc.TriggerAsync("order.placed", MakeContext());

        Assert.True(result.Success);
        emailMock.Verify(m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TriggerAsync_MultipleChannels_DispatchesBoth()
    {
        var emailMock = MakeMock("email");
        var smsMock = MakeMock("sms");
        var (svc, _) = BuildService(
            o => o.DefineEvent("order.placed", e => e.UseChannels("email", "sms")),
            n => { },
            emailMock, smsMock);

        var result = await svc.TriggerAsync("order.placed", MakeContext());

        Assert.True(result.Success);
        emailMock.Verify(m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()), Times.Once);
        smsMock.Verify(m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── TriggerAsync — unknown event ──────────────────────────────────────────

    [Fact]
    public async Task TriggerAsync_UnknownEvent_Throws()
    {
        var (svc, _) = BuildService(o => { }, n => { });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.TriggerAsync("no.such.event", MakeContext()));
    }

    // ── TriggerAsync — conditions ─────────────────────────────────────────────

    [Fact]
    public async Task TriggerAsync_ConditionFalse_SkipsChannel()
    {
        var emailMock = MakeMock("email");
        var smsMock = MakeMock("sms");
        var (svc, _) = BuildService(
            o => o.DefineEvent("order.placed", e => e
                .UseChannels("email", "sms")
                .WithCondition("sms", _ => false)),
            n => { },
            emailMock, smsMock);

        await svc.TriggerAsync("order.placed", MakeContext());

        emailMock.Verify(m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()), Times.Once);
        smsMock.Verify(m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TriggerAsync_ConditionTrue_SendsChannel()
    {
        var smsMock = MakeMock("sms");
        var (svc, _) = BuildService(
            o => o.DefineEvent("otp", e => e
                .UseChannels("sms")
                .WithCondition("sms", ctx => ctx.User.PhoneVerified)),
            n => { },
            smsMock: smsMock);

        var ctx = MakeContext();
        ctx.User.PhoneVerified = true;

        await svc.TriggerAsync("otp", ctx);

        smsMock.Verify(m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── TriggerAsync — delivery hook ──────────────────────────────────────────

    [Fact]
    public async Task TriggerAsync_DeliveryHook_CalledOncePerChannel()
    {
        var emailMock = MakeMock("email");
        var smsMock = MakeMock("sms");
        var (svc, hook) = BuildService(
            o => o.DefineEvent("order.placed", e => e.UseChannels("email", "sms")),
            n => { },
            emailMock, smsMock);

        await svc.TriggerAsync("order.placed", MakeContext());

        Assert.Equal(2, hook.Count);
    }

    [Fact]
    public async Task TriggerAsync_HookThrows_DoesNotPropagate()
    {
        var emailMock = MakeMock("email");
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new NotifyOptions
        {
            Email = new EmailOptions { Provider = "sendgrid" }
        }));
        services.AddKeyedSingleton<INotificationChannel>("email:sendgrid", (_, _) => emailMock.Object);
        services.AddLogging();
        services.AddRecurPixelNotifyOrchestrator(o =>
        {
            o.DefineEvent("test.event", e => e.UseChannels("email"));
            o.OnDelivery(_ => throw new Exception("hook exploded"));
        });

        var svc = services.BuildServiceProvider().GetRequiredService<INotifyService>();
        var ctx = new NotifyContext
        {
            User = new NotifyUser { UserId = "u1" },
            Channels = new() { ["email"] = new() { To = "a@b.com", Subject = "s", Body = "b" } }
        };

        var result = await svc.TriggerAsync("test.event", ctx);

        Assert.True(result.Success);
    }

    // ── TriggerAsync — no payload for active channel ──────────────────────────

    [Fact]
    public async Task TriggerAsync_ChannelHasNoPayload_SkipsGracefully()
    {
        var emailMock = MakeMock("email");
        var (svc, hook) = BuildService(
            o => o.DefineEvent("order.placed", e => e.UseChannels("email", "sms")),
            n => { },
            emailMock: emailMock);

        var ctx = new NotifyContext
        {
            User = new NotifyUser { UserId = "u1" },
            Channels = new() { ["email"] = new() { To = "a@b.com", Subject = "s", Body = "b" } }
        };

        var result = await svc.TriggerAsync("order.placed", ctx);

        Assert.True(result.Success);
        emailMock.Verify(m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(hook);
    }

    // ── TriggerAsync — adapter failure ────────────────────────────────────────

    [Fact]
    public async Task TriggerAsync_AdapterFails_AggregateResultIsFailure()
    {
        var emailMock = MakeMock("email", success: false, error: "SMTP timeout");
        var (svc, _) = BuildService(
            o => o.DefineEvent("order.placed", e => e.UseChannels("email")),
            n => { },
            emailMock: emailMock);

        var result = await svc.TriggerAsync("order.placed", MakeContext());

        Assert.False(result.Success);
        Assert.Contains("SMTP timeout", result.Error);
    }

    // ── Named provider routing ─────────────────────────────────────────────────

    [Fact]
    public async Task TriggerAsync_NamedProviderNotConfigured_Throws()
    {
        var emailMock = MakeMock("email");
        var (svc, _) = BuildService(
            o => o.DefineEvent("order.placed", e => e.UseChannels("email")),
            n => { },
            emailMock: emailMock);

        var ctx = new NotifyContext
        {
            User = new NotifyUser { UserId = "u1" },
            Channels = new()
            {
                ["email"] = new()
                {
                    To = "a@b.com",
                    Subject = "s",
                    Body = "b",
                    Metadata = new() { ["provider"] = "transactional" }
                }
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.TriggerAsync("order.placed", ctx));
    }

    // ── BulkTriggerAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task BulkTriggerAsync_MultipleContexts_AllDispatched()
    {
        var emailMock = MakeMock("email");
        var (svc, hook) = BuildService(
            o => o.DefineEvent("promo.blast", e => e.UseChannels("email")),
            n => { },
            emailMock: emailMock);

        var contexts = new List<NotifyContext>
        {
            new() { User = new() { UserId = "u1" }, Channels = new() { ["email"] = new() { To = "a@b.com", Subject = "s", Body = "b" } } },
            new() { User = new() { UserId = "u2" }, Channels = new() { ["email"] = new() { To = "c@d.com", Subject = "s", Body = "b" } } },
            new() { User = new() { UserId = "u3" }, Channels = new() { ["email"] = new() { To = "e@f.com", Subject = "s", Body = "b" } } }
        };

        var bulk = await svc.BulkTriggerAsync("promo.blast", contexts);

        Assert.Equal(3, bulk.Total);
        Assert.True(bulk.AllSucceeded);
        Assert.Equal(3, hook.Count);
        emailMock.Verify(m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task BulkTriggerAsync_PartialFailure_ReflectedInResult()
    {
        var callCount = 0;
        var emailMock = new Mock<INotificationChannel>();
        emailMock.Setup(m => m.ChannelName).Returns("email");
        emailMock.Setup(m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var succeed = Interlocked.Increment(ref callCount) % 2 == 1;
                return new NotifyResult
                {
                    Success = succeed,
                    Channel = "email",
                    Error = succeed ? null : "provider error"
                };
            });

        var (svc, _) = BuildService(
            o => o.DefineEvent("promo.blast", e => e.UseChannels("email")),
            n => { },
            emailMock: emailMock);

        var contexts = new List<NotifyContext>
        {
            new() { User = new() { UserId = "u1" }, Channels = new() { ["email"] = new() { To = "a@b.com", Subject = "s", Body = "b" } } },
            new() { User = new() { UserId = "u2" }, Channels = new() { ["email"] = new() { To = "c@d.com", Subject = "s", Body = "b" } } }
        };

        var bulk = await svc.BulkTriggerAsync("promo.blast", contexts);

        Assert.Equal(2, bulk.Total);
        Assert.False(bulk.AllSucceeded);
        Assert.True(bulk.AnySucceeded);
        Assert.Equal(1, bulk.FailureCount);
        Assert.Equal(1, bulk.SuccessCount);
    }

    // ── EventDefinitionBuilder ─────────────────────────────────────────────────

    [Fact]
    public void DefineEvent_DuplicateName_Throws()
    {
        var options = new OrchestratorOptions();
        options.DefineEvent("order.placed", e => e.UseChannels("email"));

        Assert.Throws<InvalidOperationException>(
            () => options.DefineEvent("order.placed", e => e.UseChannels("sms")));
    }

    [Fact]
    public void DefineEvent_EmptyName_Throws()
    {
        var options = new OrchestratorOptions();
        Assert.Throws<ArgumentException>(
            () => options.DefineEvent("", e => e.UseChannels("email")));
    }

    // ── Direct channel access ──────────────────────────────────────────────────

    [Fact]
    public async Task DirectSend_Email_BypassesEventSystem()
    {
        var emailMock = MakeMock("email");
        var (svc, hook) = BuildService(o => { }, n => { }, emailMock: emailMock);

        var result = await svc.Email.SendAsync(new NotificationPayload
        {
            To = "direct@example.com",
            Subject = "Direct",
            Body = "Direct body"
        });

        Assert.True(result.Success);
        emailMock.Verify(m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(hook); // hook not called on direct send
    }
}
