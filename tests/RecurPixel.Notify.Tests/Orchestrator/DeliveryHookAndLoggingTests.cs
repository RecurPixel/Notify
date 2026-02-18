using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Channels;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.Email.Smtp;
using RecurPixel.Notify.Orchestrator.Extensions;
using RecurPixel.Notify.Orchestrator.Options;
using RecurPixel.Notify.Orchestrator.Services;

namespace RecurPixel.Notify.Tests.Orchestrator;

// ── Test logger helper ────────────────────────────────────────────────────────

/// <summary>
/// Captures log entries for assertion in tests.
/// Only used to verify adapter-level logging — never used to assert Orchestrator internals.
/// </summary>
file sealed class TestLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel Level, string Message)> _entries = new();

    public IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add((logLevel, formatter(state, exception)));
    }

    public bool HasDebug(string fragment) =>
        _entries.Any(e => e.Level == LogLevel.Debug &&
                          e.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class DeliveryHookAndLoggingTests
{
    // ── Builder ───────────────────────────────────────────────────────────────

    private static (INotifyService service, List<NotifyResult> hookResults) BuildService(
        Action<OrchestratorOptions> configureOrch,
        Action<NotifyOptions>? configureNotify = null,
        Mock<INotificationChannel>? emailMock  = null,
        Mock<INotificationChannel>? smsMock    = null)
    {
        var hookResults = new List<NotifyResult>();

        var notifyOptions = new NotifyOptions
        {
            Email = new EmailOptions { Provider = "sendgrid" },
            Sms   = new SmsOptions   { Provider = "twilio"   }
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

        services.AddRecurPixelNotifyOrchestrator(o =>
        {
            o.OnDelivery(r => { hookResults.Add(r); return Task.CompletedTask; });
            configureOrch(o);
        });

        var svc = services.BuildServiceProvider().GetRequiredService<INotifyService>();
        return (svc, hookResults);
    }

    private static Mock<INotificationChannel> MakeMock(
        string channel,
        bool success      = true,
        string? error     = null,
        string? provider  = null,
        string? recipient = null)
    {
        var mock = new Mock<INotificationChannel>();
        mock.Setup(m => m.ChannelName).Returns(channel);
        mock.Setup(m => m.SendAsync(
                It.IsAny<NotificationPayload>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotifyResult
            {
                Success    = success,
                Channel    = channel,
                Provider   = provider ?? channel,
                Recipient  = recipient,
                Error      = error,
                SentAt     = DateTime.UtcNow
            });
        return mock;
    }

    // ── Hook field values — success ───────────────────────────────────────────

    [Fact]
    public async Task Hook_SuccessResult_ChannelAndProviderPopulated()
    {
        var emailMock = new Mock<INotificationChannel>();
        emailMock.Setup(m => m.ChannelName).Returns("email");
        emailMock.Setup(m => m.SendAsync(
                It.IsAny<NotificationPayload>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotifyResult
            {
                Success    = true,
                Channel    = "email",
                Provider   = "sendgrid",
                ProviderId = "msg_abc123",
                Recipient  = "user@example.com",
                SentAt     = DateTime.UtcNow
            });

        var (svc, hook) = BuildService(
            o => o.DefineEvent("order.placed", e => e.UseChannels("email")),
            emailMock: emailMock);

        await svc.TriggerAsync("order.placed", new NotifyContext
        {
            User     = new NotifyUser { UserId = "u1" },
            Channels = new() { ["email"] = new() { To = "user@example.com", Subject = "s", Body = "b" } }
        });

        Assert.Single(hook);
        var result = hook[0];
        Assert.True(result.Success);
        Assert.Equal("email",           result.Channel);
        Assert.Equal("sendgrid",        result.Provider);
        Assert.Equal("msg_abc123",      result.ProviderId);
        Assert.Equal("user@example.com",result.Recipient);
        Assert.Null(result.Error);
        Assert.False(result.UsedFallback);
    }

    [Fact]
    public async Task Hook_FailureResult_ErrorPopulated()
    {
        var emailMock = MakeMock("email",
            success: false,
            error: "connection refused",
            provider: "sendgrid",
            recipient: "fail@example.com");

        var (svc, hook) = BuildService(
            o => o.DefineEvent("order.placed", e => e.UseChannels("email")),
            emailMock: emailMock);

        await svc.TriggerAsync("order.placed", new NotifyContext
        {
            User     = new NotifyUser { UserId = "u1" },
            Channels = new() { ["email"] = new() { To = "fail@example.com", Subject = "s", Body = "b" } }
        });

        Assert.Single(hook);
        var result = hook[0];
        Assert.False(result.Success);
        Assert.Equal("email",              result.Channel);
        Assert.Equal("sendgrid",           result.Provider);
        Assert.Equal("connection refused", result.Error);
        Assert.Equal("fail@example.com",   result.Recipient);
        Assert.False(result.UsedFallback);
    }

    [Fact]
    public async Task Hook_SentAt_IsRecentUtc()
    {
        var before    = DateTime.UtcNow.AddSeconds(-1);
        var emailMock = MakeMock("email");

        var (svc, hook) = BuildService(
            o => o.DefineEvent("order.placed", e => e.UseChannels("email")),
            emailMock: emailMock);

        await svc.TriggerAsync("order.placed", new NotifyContext
        {
            User     = new NotifyUser { UserId = "u1" },
            Channels = new() { ["email"] = new() { To = "a@b.com", Subject = "s", Body = "b" } }
        });

        Assert.Single(hook);
        Assert.True(hook[0].SentAt >= before);
        Assert.True(hook[0].SentAt <= DateTime.UtcNow.AddSeconds(1));
    }

    // ── Hook field values — multiple channels ─────────────────────────────────

    [Fact]
    public async Task Hook_MultipleChannels_EachResultHasCorrectChannel()
    {
        var emailMock = MakeMock("email", provider: "sendgrid");
        var smsMock   = MakeMock("sms",   provider: "twilio");

        var (svc, hook) = BuildService(
            o => o.DefineEvent("order.placed", e => e.UseChannels("email", "sms")),
            emailMock: emailMock,
            smsMock: smsMock);

        await svc.TriggerAsync("order.placed", new NotifyContext
        {
            User = new NotifyUser { UserId = "u1" },
            Channels = new()
            {
                ["email"] = new() { To = "a@b.com",    Subject = "s", Body = "b" },
                ["sms"]   = new() { To = "+1234567890",               Body = "s" }
            }
        });

        Assert.Equal(2, hook.Count);

        var emailResult = hook.Single(r => r.Channel == "email");
        var smsResult   = hook.Single(r => r.Channel == "sms");

        Assert.Equal("sendgrid", emailResult.Provider);
        Assert.Equal("twilio",   smsResult.Provider);
        Assert.All(hook, r => Assert.True(r.Success));
    }

    // ── Hook — UsedFallback flag ──────────────────────────────────────────────

    [Fact]
    public async Task Hook_PrimaryResult_UsedFallbackIsFalse()
    {
        var emailMock = MakeMock("email");

        var (svc, hook) = BuildService(
            o => o.DefineEvent("order.placed", e => e.UseChannels("email")),
            emailMock: emailMock);

        await svc.TriggerAsync("order.placed", new NotifyContext
        {
            User     = new NotifyUser { UserId = "u1" },
            Channels = new() { ["email"] = new() { To = "a@b.com", Subject = "s", Body = "b" } }
        });

        Assert.False(hook[0].UsedFallback);
    }

    // ── Hook — not called on direct send ─────────────────────────────────────

    [Fact]
    public async Task Hook_DirectSend_NotCalled()
    {
        var emailMock = MakeMock("email");

        var (svc, hook) = BuildService(
            o => { },
            emailMock: emailMock);

        // Direct send bypasses the event system and therefore the hook
        await svc.Email.SendAsync(new NotificationPayload
        {
            To = "direct@example.com", Subject = "s", Body = "b"
        });

        Assert.Empty(hook);
    }

    // ── Adapter logging — SmtpChannel ────────────────────────────────────────
    // SmtpChannel is the right target for direct adapter logging tests because
    // it fails predictably without any SDK or HTTP mocking — a bad host throws
    // immediately, exercising the failure log path.

    [Fact]
    public async Task SmtpChannel_AttemptLoggedAtDebug()
    {
        var logger  = new TestLogger<SmtpChannel>();
        var channel = new SmtpChannel(
            Options.Create(new SmtpOptions
            {
                Host      = "localhost",
                Port      = 2525,
                FromEmail = "from@example.com"
            }),
            logger);

        await channel.SendAsync(new NotificationPayload
        {
            To = "to@example.com", Subject = "s", Body = "b"
        });

        // Attempt log must always fire, regardless of outcome
        Assert.True(logger.HasDebug("attempting"));
    }

    [Fact]
    public async Task SmtpChannel_FailureLoggedAtDebug()
    {
        var logger  = new TestLogger<SmtpChannel>();
        var channel = new SmtpChannel(
            Options.Create(new SmtpOptions
            {
                Host      = "127.0.0.1",
                Port      = 19999,       // nothing listening here — will throw
                FromEmail = "from@example.com"
            }),
            logger);

        var result = await channel.SendAsync(new NotificationPayload
        {
            To = "to@example.com", Subject = "s", Body = "b"
        });

        Assert.False(result.Success);
        Assert.NotNull(result.Error);

        // Failure log must fire on exception
        Assert.True(logger.HasDebug("failed"));
    }

    [Fact]
    public async Task SmtpChannel_RecipientInLogMessages()
    {
        var logger  = new TestLogger<SmtpChannel>();
        var channel = new SmtpChannel(
            Options.Create(new SmtpOptions
            {
                Host      = "127.0.0.1",
                Port      = 19999,
                FromEmail = "from@example.com"
            }),
            logger);

        await channel.SendAsync(new NotificationPayload
        {
            To = "recipient@example.com", Subject = "s", Body = "b"
        });

        // Recipient must appear in at least one log entry for traceability
        Assert.True(logger.Entries.Any(e =>
            e.Message.Contains("recipient@example.com", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task SmtpChannel_ResultContainsRecipient()
    {
        var logger  = new TestLogger<SmtpChannel>();
        var channel = new SmtpChannel(
            Options.Create(new SmtpOptions
            {
                Host      = "127.0.0.1",
                Port      = 19999,
                FromEmail = "from@example.com"
            }),
            logger);

        var result = await channel.SendAsync(new NotificationPayload
        {
            To = "fail@example.com", Subject = "s", Body = "b"
        });

        // Recipient must be set on the result so the delivery hook can surface it
        Assert.Equal("fail@example.com", result.Recipient);
        Assert.Equal("smtp",             result.Provider);
        Assert.Equal("email",            result.Channel);
    }
}
