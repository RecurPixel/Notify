using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

[ChannelAdapter("telegram", "default")]
internal sealed class TelegramRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Telegram?.BotToken);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Telegram!;
        services.Configure<TelegramOptions>(o =>
        {
            o.BotToken  = opts.BotToken;
            o.ChatId    = opts.ChatId;
            o.ParseMode = opts.ParseMode;
        });
        services.AddHttpClient("telegram:default", http =>
            http.Timeout = TimeSpan.FromSeconds(30));
        services.TryAddKeyedSingleton<INotificationChannel, TelegramChannel>("telegram:default");
    }
}
