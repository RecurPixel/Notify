using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Models;
using Xunit;

namespace RecurPixel.Notify.Tests.Core;

public class NotificationChannelBaseTests
{
    // ── Fake adapter ─────────────────────────────────────────────────────────
    // Implements SendAsync only — bulk comes free from the base class loop.
    private sealed class FakeChannel : NotificationChannelBase
    {
        public override string ChannelName => "fake";

        public List<NotificationPayload> Received { get; } = new();

        public override Task<NotifyResult> SendAsync(
            NotificationPayload payload,
            CancellationToken ct = default)
        {
            Received.Add(payload);
            return Task.FromResult(new NotifyResult
            {
                Success = true,
                Channel = ChannelName,
                Recipient = payload.To,
                SentAt = System.DateTime.UtcNow
            });
        }
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendBulkAsync_LoopsSendAsync_ForEachPayload()
    {
        var channel = new FakeChannel();
        var payloads = new List<NotificationPayload>
        {
            new() { To = "a@example.com", Subject = "Hello A", Body = "Body A" },
            new() { To = "b@example.com", Subject = "Hello B", Body = "Body B" },
            new() { To = "c@example.com", Subject = "Hello C", Body = "Body C" },
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.Equal(3, channel.Received.Count);
        Assert.Equal(3, result.Total);
    }

    [Fact]
    public async Task SendBulkAsync_SetsUsedNativeBatch_False()
    {
        var channel = new FakeChannel();
        var payloads = new List<NotificationPayload>
        {
            new() { To = "a@example.com", Body = "Hi" }
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.False(result.UsedNativeBatch);
    }

    [Fact]
    public async Task SendBulkAsync_SetsRecipient_OnEachResult()
    {
        var channel = new FakeChannel();
        var payloads = new List<NotificationPayload>
        {
            new() { To = "a@example.com", Body = "Hi" },
            new() { To = "b@example.com", Body = "Hi" },
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.Equal("a@example.com", result.Results[0].Recipient);
        Assert.Equal("b@example.com", result.Results[1].Recipient);
    }

    [Fact]
    public async Task SendBulkAsync_AllSucceeded_True_WhenAllPass()
    {
        var channel = new FakeChannel();
        var payloads = new List<NotificationPayload>
        {
            new() { To = "a@example.com", Body = "Hi" },
            new() { To = "b@example.com", Body = "Hi" },
        };

        var result = await channel.SendBulkAsync(payloads);

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
    }

    [Fact]
    public async Task SendBulkAsync_EmptyList_ReturnsEmptyResult()
    {
        var channel = new FakeChannel();
        var payloads = new List<NotificationPayload>();

        var result = await channel.SendBulkAsync(payloads);

        Assert.Equal(0, result.Total);
        Assert.True(result.AllSucceeded); // vacuously true
    }
}