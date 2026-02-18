using RecurPixel.Notify.InApp;

namespace RecurPixel.Notify.Tests;

public sealed class InAppChannelTests
{
    private static NotificationPayload DefaultPayload => new()
    {
        To = "user-id-abc123",
        Subject = "You have a new message",
        Body = "Click here to view it"
    };

    // ── success ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_HandlerReturnsSuccess_PropagatesResult()
    {
        var options = new InAppOptions
        {
            Handler = (payload, _) => Task.FromResult(new NotifyResult
            {
                Success = true,
                ProviderId = "db-row-id-999"
            })
        };

        var channel = new InAppChannel(
            Options.Create(options),
            NullLogger<InAppChannel>.Instance);

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

        var options = new InAppOptions
        {
            Handler = (_, _) => Task.FromResult(new NotifyResult
            {
                Success = true,
                SentAt = sentAt
            })
        };

        var channel = new InAppChannel(
            Options.Create(options),
            NullLogger<InAppChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(sentAt, result.SentAt);
    }

    [Fact]
    public async Task SendAsync_HandlerReturnsSentAtDefault_SetsSentAt()
    {
        var before = DateTime.UtcNow;

        var options = new InAppOptions
        {
            Handler = (_, _) => Task.FromResult(new NotifyResult
            {
                Success = true,
                SentAt = default   // channel should fill this in
            })
        };

        var channel = new InAppChannel(
            Options.Create(options),
            NullLogger<InAppChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.True(result.SentAt >= before);
    }

    [Fact]
    public async Task SendAsync_AlwaysOverridesChannelAndProvider()
    {
        var options = new InAppOptions
        {
            Handler = (_, _) => Task.FromResult(new NotifyResult
            {
                Success = true,
                Channel = "something-else",   // should be overwritten
                Provider = "something-else"    // should be overwritten
            })
        };

        var channel = new InAppChannel(
            Options.Create(options),
            NullLogger<InAppChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal("inapp", result.Channel);
        Assert.Equal("inapp", result.Provider);
    }

    [Fact]
    public async Task SendAsync_AlwaysOverridesRecipient()
    {
        var options = new InAppOptions
        {
            Handler = (_, _) => Task.FromResult(new NotifyResult
            {
                Success = true,
                Recipient = "wrong-recipient"   // should be overwritten
            })
        };

        var channel = new InAppChannel(
            Options.Create(options),
            NullLogger<InAppChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    // ── handler returns failure ───────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_HandlerReturnsFailure_PropagatesFailure()
    {
        var options = new InAppOptions
        {
            Handler = (_, _) => Task.FromResult(new NotifyResult
            {
                Success = false,
                Error = "User inbox is full"
            })
        };

        var channel = new InAppChannel(
            Options.Create(options),
            NullLogger<InAppChannel>.Instance);

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
        var options = new InAppOptions
        {
            Handler = (_, _) => throw new InvalidOperationException("DB connection lost")
        };

        var channel = new InAppChannel(
            Options.Create(options),
            NullLogger<InAppChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload);

        Assert.False(result.Success);
        Assert.Contains("DB connection lost", result.Error);
        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    [Fact]
    public async Task SendAsync_HandlerThrowsTaskCanceled_ReturnsFalse()
    {
        var options = new InAppOptions
        {
            Handler = (_, _) => throw new TaskCanceledException("Request was cancelled")
        };

        var channel = new InAppChannel(
            Options.Create(options),
            NullLogger<InAppChannel>.Instance);

        var result = await channel.SendAsync(DefaultPayload, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(DefaultPayload.To, result.Recipient);
    }

    // ── no handler configured ────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_NoHandlerConfigured_ReturnsFalseWithClearMessage()
    {
        var options = new InAppOptions { Handler = null };

        var channel = new InAppChannel(
            Options.Create(options),
            NullLogger<InAppChannel>.Instance);

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
        var channel = new InAppChannel(
            Options.Create(new InAppOptions()),
            NullLogger<InAppChannel>.Instance);

        Assert.Equal("inapp", channel.ChannelName);
    }

    [Fact]
    public async Task SendAsync_PassesPayloadToHandler()
    {
        NotificationPayload? captured = null;

        var options = new InAppOptions
        {
            Handler = (payload, _) =>
            {
                captured = payload;
                return Task.FromResult(new NotifyResult { Success = true });
            }
        };

        var channel = new InAppChannel(
            Options.Create(options),
            NullLogger<InAppChannel>.Instance);

        await channel.SendAsync(DefaultPayload);

        Assert.NotNull(captured);
        Assert.Equal(DefaultPayload.To, captured!.To);
        Assert.Equal(DefaultPayload.Subject, captured.Subject);
        Assert.Equal(DefaultPayload.Body, captured.Body);
    }

    [Fact]
    public async Task SendAsync_PassesCancellationTokenToHandler()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken? captured = null;

        var options = new InAppOptions
        {
            Handler = (_, ct) =>
            {
                captured = ct;
                return Task.FromResult(new NotifyResult { Success = true });
            }
        };

        var channel = new InAppChannel(
            Options.Create(options),
            NullLogger<InAppChannel>.Instance);

        await channel.SendAsync(DefaultPayload, cts.Token);

        Assert.Equal(cts.Token, captured);
    }
}
