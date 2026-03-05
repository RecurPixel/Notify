using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Core.Models;

/// <summary>LINE Messaging API credentials.</summary>
public class LineOptions
{
    /// <summary>Channel Access Token from the LINE Developers console.</summary>
    public string ChannelAccessToken { get; set; } = string.Empty;
}

/// <summary>Viber Business Messages API credentials.</summary>
public class ViberOptions
{
    /// <summary>Bot Authentication Token from the Viber Admin Panel.</summary>
    public string BotAuthToken { get; set; } = string.Empty;

    /// <summary>Display name shown to recipients as the message sender.</summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>Optional avatar URL shown alongside the sender name.</summary>
    public string? SenderAvatarUrl { get; set; }
}

/// <summary>
/// In-app notification options.
/// Call <see cref="UseHandler(Func{InAppNotification, Task{NotifyResult}})"/> to wire the
/// delivery implementation. Storage, SignalR, queuing — all user-owned.
/// </summary>
public class InAppOptions
{
    /// <summary>
    /// Internal handler invoked on every in-app send attempt.
    /// Receives the <see cref="InAppNotification"/> and the root <see cref="IServiceProvider"/>
    /// (use it to create a scope for scoped services).
    /// </summary>
    internal Func<InAppNotification, IServiceProvider, Task<NotifyResult>>? DeliverHandler { get; private set; }

    /// <summary>
    /// Provides the delivery implementation for in-app notifications.
    /// The handler receives a strongly-typed <see cref="InAppNotification"/> and must
    /// return a <see cref="NotifyResult"/> indicating success or failure.
    /// <para>
    /// This IS the send operation (write to your database, push via SignalR, etc.).
    /// It is distinct from <c>OrchestratorOptions.OnDelivery</c>, which is an audit
    /// callback called after every channel send attempt.
    /// </para>
    /// </summary>
    public InAppOptions UseHandler(Func<InAppNotification, Task<NotifyResult>> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));
        DeliverHandler = (notification, _) => handler(notification);
        return this;
    }

    /// <summary>
    /// Provides the delivery implementation with a scoped <typeparamref name="TService"/>
    /// resolved from a new DI scope for each invocation.
    /// Use this to inject a scoped <c>DbContext</c> or other scoped service.
    /// </summary>
    public InAppOptions UseHandler<TService>(Func<InAppNotification, TService, Task<NotifyResult>> handler)
        where TService : class
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));
        DeliverHandler = async (notification, sp) =>
        {
            using var scope = sp.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<TService>();
            return await handler(notification, service);
        };
        return this;
    }
}

/// <summary>Vonage WhatsApp Business messaging credentials.</summary>
public class VonageWhatsAppOptions
{
    /// <summary>Vonage API key from the Vonage Dashboard.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Vonage API secret from the Vonage Dashboard.</summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>WhatsApp sender number registered with Vonage.</summary>
    public string FromNumber { get; set; } = string.Empty;
}

/// <summary>Azure Communication Services Email credentials.</summary>
public class AzureCommEmailOptions
{
    /// <summary>Azure Communication Services connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Sender email address registered in ACS.</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>Optional display name for the sender.</summary>
    public string? FromName { get; set; }
}

/// <summary>Azure Communication Services SMS credentials.</summary>
public class AzureCommSmsOptions
{
    /// <summary>Azure Communication Services connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Phone number purchased from ACS to send from.</summary>
    public string FromNumber { get; set; } = string.Empty;
}

/// <summary>Mattermost incoming webhook credentials.</summary>
public class MattermostOptions
{
    /// <summary>Incoming webhook URL from Mattermost integrations settings.</summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>Optional override for the bot username shown in Mattermost.</summary>
    public string? Username { get; set; }

    /// <summary>Optional override for the channel to post to (e.g. "town-square").</summary>
    public string? Channel { get; set; }
}

/// <summary>Rocket.Chat incoming webhook credentials.</summary>
public class RocketChatOptions
{
    /// <summary>Incoming webhook URL from Rocket.Chat administration settings.</summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>Optional override for the bot username shown in Rocket.Chat.</summary>
    public string? Username { get; set; }

    /// <summary>Optional override for the channel to post to (e.g. "#general").</summary>
    public string? Channel { get; set; }
}
