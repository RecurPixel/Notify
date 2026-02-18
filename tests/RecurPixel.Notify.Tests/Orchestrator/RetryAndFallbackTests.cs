using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Options.Channels;
using RecurPixel.Notify.Orchestrator.Extensions;
using RecurPixel.Notify.Orchestrator.Options;
using RecurPixel.Notify.Orchestrator.Services;

namespace RecurPixel.Notify.Tests.Orchestrator;

public class RetryAndFallbackTests
{
    // ── Builder ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a fully-wired INotifyService with optional email, sms, and slack mocks.
    /// Slack is a simple channel (no provider selection) — keyed as "slack".
    /// </summary>
    private static (INotifyService service, List<NotifyResult> hookResults) BuildService(
        Action<OrchestratorOptions> configureOrch,
        Action<NotifyOptions>? configureNotify = null,
        Mock<INotificationChannel>? emailMock = null,
        Mock<INotificationChannel>? smsMock = null,
        Mock<INotificationChannel>? slackMock = null)
    {
        var hookResults = new List<NotifyResult>();

        var notifyOptions = new NotifyOptions
        {
            Email = new EmailOptions { Provider = "sendgrid" },
            Sms = new SmsOptions { Provider = "twilio" }
        };
        configureNotify?.Invoke(notifyOptions);

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(notifyOptions));
        services.AddLogging();

        if (emailMock is not null)
            services.AddKeyedSingleton<INotificationChannel>(
                "email:sendgrid", (_, _) => emailMock.Object);

        if (smsMock is not null)
            services.AddKeyedSingleton<INotificationChannel>(
                "sms:twilio", (_, _) => smsMock.Object);

        // Slack has no provider selection — keyed by channel name alone
        if (slackMock is not null)
            services.AddKeyedSingleton<INotificationChannel>(
                "slack", (_, _) => slackMock.Object);

        services.AddRecurPixelNotifyOrchestrator(o =>
        {
            o.OnDelivery(r => { hookResults.Add(r); return Task.CompletedTask; });
            configureOrch(o);
        });

        var svc = services.BuildServiceProvider().GetRequiredService<INotifyService>();
        return (svc, hookResults);
    }

    /// <summary>Creates a mock that always returns the given success/failure.</summary>
    private static Mock<INotificationChannel> MakeMock(
        string channel, bool success = true, string? error = null)
    {
        var mock = new Mock<INotificationChannel>();
        mock.Setup(m => m.ChannelName).Returns(channel);
        mock.Setup(m => m.SendAsync(
                It.IsAny<NotificationPayload>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotifyResult
            {
                Success = success,
                Channel = channel,
                Provider = channel,
                Error = error,
                SentAt = DateTime.UtcNow
            });
        return mock;
    }

    /// <summary>
    /// Creates a mock that fails for the first <paramref name="failCount"/> calls,
    /// then succeeds.
    /// </summary>
    private static Mock<INotificationChannel> MakeFailThenSucceedMock(
        string channel, int failCount)
    {
        var callCount = 0;
        var mock = new Mock<INotificationChannel>();
        mock.Setup(m => m.ChannelName).Returns(channel);
        mock.Setup(m => m.SendAsync(
                It.IsAny<NotificationPayload>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var attempt = Interlocked.Increment(ref callCount);
                var succeed = attempt > failCount;
                return new NotifyResult
                {
                    Success = succeed,
                    Channel = channel,
                    Provider = channel,
                    Error = succeed ? null : $"transient error attempt {attempt}",
                    SentAt = DateTime.UtcNow
                };
            });
        return mock;
    }

    private static NotifyContext MakeContext(bool includeSlack = false)
    {
        var channels = new Dictionary<string, NotificationPayload>
        {
            ["email"] = new() { To = "a@b.com", Subject = "Hello", Body = "Body" },
            ["sms"] = new() { To = "+1234567890", Body = "SMS" }
        };

        if (includeSlack)
            channels["slack"] = new() { To = "#alerts", Body = "Fallback alert" };

        return new NotifyContext
        {
            User = new NotifyUser { UserId = "u1", Email = "a@b.com", Phone = "+1234567890" },
            Channels = channels
        };
    }

    // ── Retry — basic ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Retry_AlwaysFails_AdapterCalledMaxAttemptsTimes()
    {
        var emailMock = MakeMock("email", success: false, error: "timeout");

        var (svc, _) = BuildService(o => o
            .DefineEvent("order.placed", e => e
                .UseChannels("email")
                .WithRetry(maxAttempts: 3, delayMs: 0)),
            emailMock: emailMock);

        var result = await svc.TriggerAsync("order.placed", MakeContext());

        Assert.False(result.Success);
        emailMock.Verify(
            m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task Retry_SucceedsOnSecondAttempt_ReturnsSuccess()
    {
        // Fails once, succeeds on attempt 2
        var emailMock = MakeFailThenSucceedMock("email", failCount: 1);

        var (svc, hook) = BuildService(o => o
            .DefineEvent("order.placed", e => e
                .UseChannels("email")
                .WithRetry(maxAttempts: 3, delayMs: 0)),
            emailMock: emailMock);

        var result = await svc.TriggerAsync("order.placed", MakeContext());

        Assert.True(result.Success);
        // Adapter called twice: fail, then succeed
        emailMock.Verify(
            m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        // Hook called once for the final successful result
        Assert.Single(hook);
        Assert.True(hook[0].Success);
    }

    [Fact]
    public async Task Retry_SucceedsOnFirstAttempt_AdapterCalledOnce()
    {
        var emailMock = MakeMock("email", success: true);

        var (svc, _) = BuildService(o => o
            .DefineEvent("order.placed", e => e
                .UseChannels("email")
                .WithRetry(maxAttempts: 5, delayMs: 0)),
            emailMock: emailMock);

        await svc.TriggerAsync("order.placed", MakeContext());

        emailMock.Verify(
            m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Retry — global vs per-event ───────────────────────────────────────────

    [Fact]
    public async Task Retry_GlobalRetryUsed_WhenNoPerEventRetry()
    {
        var emailMock = MakeMock("email", success: false, error: "timeout");

        var (svc, _) = BuildService(
            o => o.DefineEvent("order.placed", e => e.UseChannels("email")),
            n => n.Retry = new RetryOptions { MaxAttempts = 4, DelayMs = 0, ExponentialBackoff = false },
            emailMock: emailMock);

        await svc.TriggerAsync("order.placed", MakeContext());

        // Global retry of 4 applies because no per-event retry is set
        emailMock.Verify(
            m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }

    [Fact]
    public async Task Retry_PerEventOverridesGlobal()
    {
        var emailMock = MakeMock("email", success: false, error: "timeout");

        var (svc, _) = BuildService(
            o => o.DefineEvent("order.placed", e => e
                .UseChannels("email")
                .WithRetry(maxAttempts: 2, delayMs: 0)),
            n => n.Retry = new RetryOptions { MaxAttempts = 5, DelayMs = 0, ExponentialBackoff = false },
            emailMock: emailMock);

        await svc.TriggerAsync("order.placed", MakeContext());

        // Per-event MaxAttempts=2 wins over global MaxAttempts=5
        emailMock.Verify(
            m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Retry_NoRetryConfig_SingleAttemptOnly()
    {
        var emailMock = MakeMock("email", success: false, error: "timeout");

        // No global retry, no per-event retry
        var (svc, _) = BuildService(
            o => o.DefineEvent("order.placed", e => e.UseChannels("email")),
            emailMock: emailMock);

        await svc.TriggerAsync("order.placed", MakeContext());

        emailMock.Verify(
            m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Cross-channel fallback ────────────────────────────────────────────────

    [Fact]
    public async Task Fallback_PrimaryFails_FallbackChannelSucceeds()
    {
        var emailMock = MakeMock("email", success: false, error: "provider down");
        var slackMock = MakeMock("slack", success: true);

        var (svc, hook) = BuildService(
            o => o.DefineEvent("alert", e => e
                .UseChannels("email")
                .WithFallback("slack")
                .WithRetry(maxAttempts: 1, delayMs: 0)),
            emailMock: emailMock,
            slackMock: slackMock);

        var result = await svc.TriggerAsync("alert", MakeContext(includeSlack: true));

        Assert.True(result.Success);
        Assert.True(result.UsedFallback);

        // Primary email attempted once
        emailMock.Verify(
            m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // Fallback slack attempted once
        slackMock.Verify(
            m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // Hook called for primary failure + fallback success = 2 calls
        Assert.Equal(2, hook.Count);
        Assert.False(hook[0].Success);   // primary email
        Assert.True(hook[1].Success);    // fallback slack
        Assert.True(hook[1].UsedFallback);
    }

    [Fact]
    public async Task Fallback_PrimarySucceeds_FallbackNotAttempted()
    {
        var emailMock = MakeMock("email", success: true);
        var slackMock = MakeMock("slack", success: true);

        var (svc, _) = BuildService(
            o => o.DefineEvent("alert", e => e
                .UseChannels("email")
                .WithFallback("slack")),
            emailMock: emailMock,
            slackMock: slackMock);

        var result = await svc.TriggerAsync("alert", MakeContext(includeSlack: true));

        Assert.True(result.Success);
        slackMock.Verify(
            m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Fallback_SkipsAlreadyDispatchedChannels()
    {
        // Both email and sms are primary channels; fallback chain lists sms first
        // then slack. sms should be skipped — slack should run.
        var emailMock = MakeMock("email", success: false, error: "down");
        var smsMock = MakeMock("sms", success: false, error: "down");
        var slackMock = MakeMock("slack", success: true);

        var (svc, _) = BuildService(
            o => o.DefineEvent("alert", e => e
                .UseChannels("email", "sms")
                .WithFallback("sms", "slack")   // sms already dispatched — must be skipped
                .WithRetry(maxAttempts: 1, delayMs: 0)),
            emailMock: emailMock,
            smsMock: smsMock,
            slackMock: slackMock);

        var result = await svc.TriggerAsync("alert", MakeContext(includeSlack: true));

        Assert.True(result.Success);
        Assert.True(result.UsedFallback);

        // sms was primary — called once there, must NOT be called again in fallback
        smsMock.Verify(
            m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // slack is the first non-already-dispatched fallback channel
        slackMock.Verify(
            m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Fallback_SkipsChannelWithNoPayloadInContext()
    {
        var emailMock = MakeMock("email", success: false, error: "down");
        var slackMock = MakeMock("slack", success: true);

        var (svc, _) = BuildService(
            o => o.DefineEvent("alert", e => e
                .UseChannels("email")
                .WithFallback("sms", "slack")   // sms has no payload in context
                .WithRetry(maxAttempts: 1, delayMs: 0)),
            emailMock: emailMock,
            slackMock: slackMock);

        // Context has email and slack — no sms payload
        var ctx = new NotifyContext
        {
            User = new NotifyUser { UserId = "u1" },
            Channels = new Dictionary<string, NotificationPayload>
            {
                ["email"] = new() { To = "a@b.com", Subject = "s", Body = "b" },
                ["slack"] = new() { To = "#alerts", Body = "fallback" }
            }
        };

        var result = await svc.TriggerAsync("alert", ctx);

        Assert.True(result.Success);
        // slack picked up after sms skipped for missing payload
        slackMock.Verify(
            m => m.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Fallback_AllFallbackChannelsFail_ResultIsFailure()
    {
        var emailMock = MakeMock("email", success: false, error: "email down");
        var slackMock = MakeMock("slack", success: false, error: "slack down");

        var (svc, hook) = BuildService(
            o => o.DefineEvent("alert", e => e
                .UseChannels("email")
                .WithFallback("slack")
                .WithRetry(maxAttempts: 1, delayMs: 0)),
            emailMock: emailMock,
            slackMock: slackMock);

        var result = await svc.TriggerAsync("alert", MakeContext(includeSlack: true));

        Assert.False(result.Success);
        // Hook called for primary failure and fallback failure
        Assert.Equal(2, hook.Count);
        Assert.All(hook, r => Assert.False(r.Success));
    }

    [Fact]
    public async Task Fallback_HookCalledForEachFallbackAttempt()
    {
        var emailMock = MakeMock("email", success: false, error: "down");
        var smsMock = MakeMock("sms", success: false, error: "down");
        var slackMock = MakeMock("slack", success: true);

        // Only email is the primary channel; sms and slack are in the fallback chain
        var (svc, hook) = BuildService(
            o => o.DefineEvent("alert", e => e
                .UseChannels("email")
                .WithFallback("sms", "slack")
                .WithRetry(maxAttempts: 1, delayMs: 0)),
            emailMock: emailMock,
            smsMock: smsMock,
            slackMock: slackMock);

        var ctx = new NotifyContext
        {
            User = new NotifyUser { UserId = "u1" },
            Channels = new Dictionary<string, NotificationPayload>
            {
                ["email"] = new() { To = "a@b.com", Subject = "s", Body = "b" },
                ["sms"] = new() { To = "+1", Body = "sms" },
                ["slack"] = new() { To = "#ch", Body = "slack" }
            }
        };

        var result = await svc.TriggerAsync("alert", ctx);

        Assert.True(result.Success);
        // Hook: email (fail) + sms fallback (fail) + slack fallback (success) = 3 calls
        Assert.Equal(3, hook.Count);
        Assert.False(hook[0].Success);  // primary email
        Assert.True(hook[1].UsedFallback);   // sms fallback attempt
        Assert.False(hook[1].Success);
        Assert.True(hook[2].UsedFallback);   // slack fallback success
        Assert.True(hook[2].Success);
    }

    [Fact]
    public async Task Fallback_UsedFallbackFlag_SetOnFallbackResult()
    {
        var emailMock = MakeMock("email", success: false, error: "down");
        var slackMock = MakeMock("slack", success: true);

        var (svc, hook) = BuildService(
            o => o.DefineEvent("alert", e => e
                .UseChannels("email")
                .WithFallback("slack")
                .WithRetry(maxAttempts: 1, delayMs: 0)),
            emailMock: emailMock,
            slackMock: slackMock);

        var result = await svc.TriggerAsync("alert", MakeContext(includeSlack: true));

        Assert.True(result.UsedFallback);

        // Primary email result: UsedFallback = false
        var emailHookResult = hook.First(r => r.Channel == "email");
        Assert.False(emailHookResult.UsedFallback);

        // Fallback slack result: UsedFallback = true
        var slackHookResult = hook.First(r => r.Channel == "slack");
        Assert.True(slackHookResult.UsedFallback);
    }
}
