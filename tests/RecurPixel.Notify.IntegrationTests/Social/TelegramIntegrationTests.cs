using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;
using RecurPixel.Notify.Core.Options.Providers;
using RecurPixel.Notify.IntegrationTests.Infrastructure;
using RecurPixel.Notify.Telegram;

namespace RecurPixel.Notify.IntegrationTests.Social;

/// <summary>
/// Integration tests for the Telegram Bot API adapter.
///
/// CREDENTIALS REQUIRED in appsettings.integration.json:
///   Notify:Telegram:BotToken   (from @BotFather on Telegram)
///   Notify:Telegram:ChatId     (your personal chat ID or group chat ID)
///
/// HOW TO GET:
///   1. Open Telegram → search @BotFather → send /newbot → follow prompts
///   2. BotFather gives you the API token — paste as BotToken
///   3. Get your Chat ID: message your bot, then open:
///      https://api.telegram.org/bot{TOKEN}/getUpdates
///      Look for: "chat":{"id": YOUR_CHAT_ID}
/// </summary>
public sealed class TelegramIntegrationTests : ChannelIntegrationTest
{
    protected override string ServiceKey => "telegram:telegram";

    protected override bool IsConfigured() =>
        TestConfiguration.HasAll(
            "Notify:Telegram:BotToken",
            "Notify:Telegram:ChatId");

    protected override void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var opts = new TelegramOptions
        {
            BotToken = config["Notify:Telegram:BotToken"]!,
            ChatId = config["Notify:Telegram:ChatId"]
        };
        services.AddTelegramChannel(opts);
    }

    protected override NotificationPayload BuildPayload(IConfiguration config) => new()
    {
        To = config["Notify:Telegram:ChatId"]!,
        Body = $"RecurPixel.Notify integration test — Telegram — {DateTime.UtcNow:HH:mm:ss} UTC"
    };
}
