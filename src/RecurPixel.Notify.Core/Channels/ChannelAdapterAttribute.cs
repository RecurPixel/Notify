using System;

namespace RecurPixel.Notify.Channels;

/// <summary>
/// Marks a class as a notification channel adapter and declares its logical channel name and provider.
/// Applied to every <see cref="INotificationChannel"/> implementation.
/// Used by the auto-registration scanner in <c>AddRecurPixelNotify()</c> to discover and register
/// adapters from loaded assemblies without requiring explicit <c>Add{Provider}Channel()</c> calls.
/// </summary>
/// <example>
/// Multi-provider channel:
/// <code>[ChannelAdapter("email", "sendgrid")]</code>
/// <code>[ChannelAdapter("sms", "twilio")]</code>
/// Single-implementation channel:
/// <code>[ChannelAdapter("slack", "default")]</code>
/// <code>[ChannelAdapter("inapp", "default")]</code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ChannelAdapterAttribute : Attribute
{
    /// <summary>
    /// Logical channel name — lowercase, e.g. <c>"email"</c>, <c>"sms"</c>, <c>"slack"</c>.
    /// </summary>
    public string Channel { get; }

    /// <summary>
    /// Provider name for multi-provider channels — e.g. <c>"sendgrid"</c>, <c>"twilio"</c>, <c>"fcm"</c>.
    /// Use <c>"default"</c> for single-implementation channels — e.g. <c>"slack"</c>, <c>"discord"</c>, <c>"inapp"</c>.
    /// </summary>
    public string Provider { get; }

    /// <summary>
    /// Initialises a new <see cref="ChannelAdapterAttribute"/>.
    /// </summary>
    /// <param name="channel">Logical channel name (lowercase). E.g. <c>"email"</c>, <c>"sms"</c>.</param>
    /// <param name="provider">Provider name. E.g. <c>"sendgrid"</c> or <c>"default"</c> for single-impl channels.</param>
    public ChannelAdapterAttribute(string channel, string provider)
    {
        Channel  = channel;
        Provider = provider;
    }
}
