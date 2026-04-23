using System;

namespace RecurPixel.Notify.Channels;

/// <summary>
/// Declares the logical channel name and provider for an <see cref="IAdapterRegistrar"/>
/// implementation. Applied to every registrar class — not to the channel class itself.
/// The auto-registration scanner in <c>AddRecurPixelNotify()</c> discovers types with this
/// attribute that implement <see cref="IAdapterRegistrar"/> and delegates all registration to them.
/// </summary>
/// <example>
/// Multi-provider channel:
/// <code>[ChannelAdapter("email", "sendgrid")] internal sealed class SendGridRegistrar : IAdapterRegistrar</code>
/// <code>[ChannelAdapter("sms", "twilio")]    internal sealed class TwilioSmsRegistrar : IAdapterRegistrar</code>
/// Single-implementation channel:
/// <code>[ChannelAdapter("slack", "default")]  internal sealed class SlackRegistrar : IAdapterRegistrar</code>
/// <code>[ChannelAdapter("inapp", "default")]  internal sealed class InAppRegistrar  : IAdapterRegistrar</code>
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
