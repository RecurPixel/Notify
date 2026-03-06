using Microsoft.Extensions.DependencyInjection;

namespace RecurPixel.Notify.Tests;

public sealed class InAppChannelTests
{
    private static readonly IServiceProvider EmptySp =
        new ServiceCollection().BuildServiceProvider();

    private static NotificationPayload DefaultPayload => new()
    {
        To = "user-id-abc123",
        Subject = "You have a new message",
        Body = "Click here to view it"
    };

    private static InAppChannel BuildChannel(Action<InAppOptions>? configure = null)
    {
        var opts = new InAppOptions();
        configure?.Invoke(opts);
        return new InAppChannel(Options.Create(opts), EmptySp, NullLogger<InAppChannel>.Instance);
    }

    // ── success ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_HandlerReturnsSuccess_PropagatesResult()
    {
        var channel = BuildChannel(o => o.UseHandler(_ => Task.FromResult(new NotifyResult
        {
            Success = true,
            ProviderId = "db-row-id-999"
        })));

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.Success);
        Assert.Equal("inapp", result.Channel);
        Assert.Equal("inapp", result.Provider);
        Assert.Equal("db-row-id-999", result.ProviderId);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_HandlerReturnsSentAt_PreservesSentAt()
    {
        var sentAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        var channel = BuildChannel(o => o.UseHandler(_ => Task.FromResult(new NotifyResult
        {
            Success = true,
            SentAt = sentAt
        })));

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(sentAt, result.SentAt);
    }

    [Fact]
    public async Task SendAsync_HandlerReturnsSentAtDefault_SetsSentAt()
    {
        var before = DateTime.UtcNow;

        var channel = BuildChannel(o => o.UseHandler(_ => Task.FromResult(new NotifyResult
        {
            Success = true,
            SentAt = default   // channel should fill this in
        })));

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.SentAt >= before);
    }

    [Fact]
    public async Task SendAsync_AlwaysOverridesChannelAndProvider()
    {
        var channel = BuildChannel(o => o.UseHandler(_ => Task.FromResult(new NotifyResult
        {
            Success = true,
            Channel = "something-else",   // should be overwritten
            Provider = "something-else"    // should be overwritten
        })));

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("inapp", result.Channel);
        Assert.Equal("inapp", result.Provider);
    }

    [Fact]
    public async Task SendAsync_AlwaysOverridesRecipient()
    {
        var channel = BuildChannel(o => o.UseHandler(_ => Task.FromResult(new NotifyResult
        {
            Success = true,
            Recipient = "wrong-recipient"   // should be overwritten
        })));

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    // ── handler returns failure ───────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_HandlerReturnsFailure_PropagatesFailure()
    {
        var channel = BuildChannel(o => o.UseHandler(_ => Task.FromResult(new NotifyResult
        {
            Success = false,
            Error = "User inbox is full"
        })));

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Equal("inapp", result.Channel);
        Assert.Equal("User inbox is full", result.Error);
        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    // ── handler throws ───────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_HandlerThrows_ReturnsFalseWithExceptionMessage()
    {
        var channel = BuildChannel(o => o.UseHandler(
            _ => throw new InvalidOperationException("DB connection lost")));

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("DB connection lost", result.Error);
        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_HandlerThrowsTaskCanceled_ReturnsFalse()
    {
        var channel = BuildChannel(o => o.UseHandler(
            _ => throw new TaskCanceledException("Request was cancelled")));

        var result = await channel.SendAsync(DefaultPayload, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    // ── no handler configured ────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_NoHandlerConfigured_ReturnsFalseWithClearMessage()
    {
        var channel = BuildChannel(); // no OnDeliver

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Equal("inapp", result.Channel);
        Assert.Equal(DefaultPayload.To, result.Recipient);
        Assert.Contains("not configured", result.Error);
    }

    // ── contract ─────────────────────────────────────────────────────────────

    [Fact]
    public void ChannelName_IsInApp()
    {
        Assert.Equal("inapp", BuildChannel().ChannelName);
    }

    [Fact]
    public async Task SendAsync_MapsPayloadToNotification()
    {
        InAppNotification? captured = null;

        var channel = BuildChannel(o => o.UseHandler(n =>
        {
            captured = n;
            return Task.FromResult(new NotifyResult { Success = true });
        }));

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(captured);
        Assert.Equal(DefaultPayload.To, captured!.UserId);
        Assert.Equal(DefaultPayload.Subject, captured.Subject);
        Assert.Equal(DefaultPayload.Body, captured.Body);
    }
}
