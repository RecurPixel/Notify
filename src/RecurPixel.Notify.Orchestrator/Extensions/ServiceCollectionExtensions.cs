using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Extensions;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Orchestrator.Dispatch;
using RecurPixel.Notify.Orchestrator.Options;
using RecurPixel.Notify.Orchestrator.Services;

namespace RecurPixel.Notify.Orchestrator.Extensions;

/// <summary>
/// Extension methods for registering RecurPixel.Notify with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    // ── Combined single-call setup (recommended) ──────────────────────────────

    /// <summary>
    /// Registers RecurPixel.Notify in a single call: Core options, auto-discovered channel
    /// adapters (filtered by configuration), and the Orchestrator.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure <see cref="NotifyOptions"/>.</param>
    /// <param name="configureOrchestrator">Action to define events and delivery hooks.</param>
    public static IServiceCollection AddRecurPixelNotify(
        this IServiceCollection services,
        Action<NotifyOptions> configureOptions,
        Action<OrchestratorOptions> configureOrchestrator)
    {
        var notifyOptions = new NotifyOptions();
        configureOptions(notifyOptions);

        // Register the raw POCO (also registers IOptions<NotifyOptions> inside orchestrator below)
        services.AddNotifyOptions(notifyOptions);

        // Auto-discover and register adapters filtered by configuration
        var registeredKeys = RegisterAdapters(services, notifyOptions);
        ValidateActiveProviders(notifyOptions, registeredKeys);

        return services.AddRecurPixelNotifyOrchestrator(configureOrchestrator);
    }

    // ── Orchestrator-only setup (use after AddRecurPixelNotify from Core) ─────

    /// <summary>
    /// Registers the Orchestrator, event registry, channel dispatcher, and
    /// <see cref="INotifyService"/>. Call after <c>AddRecurPixelNotify()</c> from Core,
    /// or use the combined <c>AddRecurPixelNotify(configureOptions, configureOrchestrator)</c>
    /// overload instead.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to define events and set delivery hooks.</param>
    public static IServiceCollection AddRecurPixelNotifyOrchestrator(
        this IServiceCollection services,
        Action<OrchestratorOptions>? configure = null)
    {
        var options = new OrchestratorOptions();
        configure?.Invoke(options);

        // Register options and registry as singletons — built once at startup
        services.AddSingleton(options);
        services.AddSingleton(options.Registry);

        // Register IOptions<NotifyOptions> wrapping the raw NotifyOptions POCO registered by Core.
        // TryAdd is a no-op if already registered.
        services.TryAddSingleton<IOptions<NotifyOptions>>(sp =>
            Microsoft.Extensions.Options.Options.Create(sp.GetRequiredService<NotifyOptions>()));

        // ChannelDispatcher is scoped — resolves scoped IServiceProvider correctly
        services.AddScoped<ChannelDispatcher>();

        // INotifyService is the primary user-facing service
        services.AddScoped<INotifyService, NotifyService>();

        return services;
    }

    // ── Adapter scanner + config filter ───────────────────────────────────────

    /// <summary>
    /// Scans all loaded assemblies for types decorated with <see cref="ChannelAdapterAttribute"/>
    /// that implement <see cref="INotificationChannel"/>.
    /// Guards against <see cref="ReflectionTypeLoadException"/> from partially loaded assemblies.
    /// </summary>
    private static IEnumerable<Type> DiscoverAdapters()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            IEnumerable<Type> types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null)!;
            }

            foreach (var type in types)
            {
                if (!type.IsClass || type.IsAbstract) continue;
                if (!typeof(INotificationChannel).IsAssignableFrom(type)) continue;
                if (type.GetCustomAttribute<ChannelAdapterAttribute>() is null) continue;
                yield return type;
            }
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when the minimum required credential for
    /// <paramref name="channel"/>/<paramref name="provider"/> is present in
    /// <paramref name="options"/>. Returns <see langword="false"/> to silently
    /// skip adapters whose options section is absent or empty.
    /// </summary>
    private static bool IsAdapterConfigured(NotifyOptions options, string channel, string provider)
        => channel switch
        {
            "email" => provider switch
            {
                "sendgrid"       => !string.IsNullOrEmpty(options.Email?.SendGrid?.ApiKey),
                "smtp"           => !string.IsNullOrEmpty(options.Email?.Smtp?.Host),
                "mailgun"        => !string.IsNullOrEmpty(options.Email?.Mailgun?.ApiKey),
                "resend"         => !string.IsNullOrEmpty(options.Email?.Resend?.ApiKey),
                "postmark"       => !string.IsNullOrEmpty(options.Email?.Postmark?.ApiKey),
                "awsses"         => !string.IsNullOrEmpty(options.Email?.AwsSes?.AccessKey),
                "azurecommemail" => !string.IsNullOrEmpty(options.Email?.AzureCommEmail?.ConnectionString),
                _                => false
            },
            "sms" => provider switch
            {
                "twilio"       => !string.IsNullOrEmpty(options.Sms?.Twilio?.AccountSid),
                "vonage"       => !string.IsNullOrEmpty(options.Sms?.Vonage?.ApiKey),
                "plivo"        => !string.IsNullOrEmpty(options.Sms?.Plivo?.AuthId),
                "sinch"        => !string.IsNullOrEmpty(options.Sms?.Sinch?.ServicePlanId),
                "messagebird"  => !string.IsNullOrEmpty(options.Sms?.MessageBird?.ApiKey),
                "awssns"       => !string.IsNullOrEmpty(options.Sms?.AwsSns?.AccessKey),
                "azurecommsms" => !string.IsNullOrEmpty(options.Sms?.AzureCommSms?.ConnectionString),
                _              => false
            },
            "push" => provider switch
            {
                "fcm"       => !string.IsNullOrEmpty(options.Push?.Fcm?.ProjectId),
                "apns"      => !string.IsNullOrEmpty(options.Push?.Apns?.KeyId),
                "onesignal" => !string.IsNullOrEmpty(options.Push?.OneSignal?.AppId),
                "expo"      => options.Push?.Expo is not null,
                _           => false
            },
            "whatsapp" => provider switch
            {
                "twilio"    => !string.IsNullOrEmpty(options.WhatsApp?.Twilio?.AccountSid),
                "metacloud" => !string.IsNullOrEmpty(options.WhatsApp?.MetaCloud?.AccessToken),
                "vonage"    => !string.IsNullOrEmpty(options.WhatsApp?.Vonage?.ApiKey),
                _           => false
            },
            "slack"       => !string.IsNullOrEmpty(options.Slack?.WebhookUrl) || !string.IsNullOrEmpty(options.Slack?.BotToken),
            "discord"     => !string.IsNullOrEmpty(options.Discord?.WebhookUrl),
            "teams"       => !string.IsNullOrEmpty(options.Teams?.WebhookUrl),
            "telegram"    => !string.IsNullOrEmpty(options.Telegram?.BotToken),
            "facebook"    => !string.IsNullOrEmpty(options.Facebook?.PageAccessToken),
            "line"        => !string.IsNullOrEmpty(options.Line?.ChannelAccessToken),
            "viber"       => !string.IsNullOrEmpty(options.Viber?.BotAuthToken),
            "mattermost"  => !string.IsNullOrEmpty(options.Mattermost?.WebhookUrl),
            "rocketchat"  => !string.IsNullOrEmpty(options.RocketChat?.WebhookUrl),
            "inapp"       => true,  // Always registered; handler wired by AddInAppChannel / OnDeliver
            _             => false
        };

    /// <summary>
    /// Discovers adapters, filters by config, and registers passing adapters as
    /// <see cref="INotificationChannel"/> keyed singletons using <c>TryAdd</c>
    /// (idempotent with any explicit <c>Add{X}Channel()</c> calls).
    /// Returns the list of registered keys for validation.
    /// </summary>
    /// <summary>
    /// Loads all <c>RecurPixel.Notify.*.dll</c> assemblies found in the application base
    /// directory that are not already loaded. .NET loads assemblies on demand, so adapter
    /// DLLs may not yet be in the AppDomain when the scanner runs at startup.
    /// </summary>
    private static void EnsureAdapterAssembliesLoaded()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var loaded  = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetName().Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dll in Directory.GetFiles(baseDir, "RecurPixel.Notify.*.dll", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileNameWithoutExtension(dll);
            if (loaded.Contains(name)) continue;
            try { Assembly.LoadFrom(dll); }
            catch { /* ignore — unloadable assemblies are simply not scanned */ }
        }
    }

    private static List<string> RegisterAdapters(IServiceCollection services, NotifyOptions options)
    {
        EnsureAdapterAssembliesLoaded();
        var registered = new List<string>();

        foreach (var type in DiscoverAdapters())
        {
            var attr = type.GetCustomAttribute<ChannelAdapterAttribute>()!;
            var key  = $"{attr.Channel}:{attr.Provider}";

            if (!IsAdapterConfigured(options, attr.Channel, attr.Provider))
                continue;

            services.TryAddKeyedSingleton(typeof(INotificationChannel), key, type);
            registered.Add(key);
        }

        return registered;
    }

    /// <summary>
    /// Validates that every active provider declared in <paramref name="options"/>
    /// has a registered adapter. Throws <see cref="InvalidOperationException"/> at
    /// startup when a <c>Provider</c> value has no matching credentials.
    /// </summary>
    private static void ValidateActiveProviders(NotifyOptions options, IReadOnlyList<string> registeredKeys)
    {
        static bool HasKey(IReadOnlyList<string> keys, string key)
            => keys.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));

        static void Check(string? provider, string channel, string sectionLabel, IReadOnlyList<string> keys)
        {
            if (string.IsNullOrEmpty(provider)) return;
            if (!HasKey(keys, $"{channel}:{provider}"))
                throw new InvalidOperationException(
                    $"Notify:{sectionLabel}:Provider is set to '{provider}' " +
                    $"but no credentials were found for '{provider}'. " +
                    $"Add the required configuration under Notify:{sectionLabel}:{char.ToUpperInvariant(provider[0]) + provider[1..]}.");
        }

        Check(options.Email?.Provider,    "email",    "Email",    registeredKeys);
        Check(options.Sms?.Provider,      "sms",      "Sms",      registeredKeys);
        Check(options.Push?.Provider,     "push",     "Push",     registeredKeys);
        Check(options.WhatsApp?.Provider, "whatsapp", "WhatsApp", registeredKeys);

        // Validate named provider routing tables
        static void CheckProviders(
            Dictionary<string, NamedProviderDefinition>? providers,
            string channel,
            IReadOnlyList<string> keys)
        {
            if (providers is null) return;
            foreach (var (name, def) in providers)
            {
                if (string.IsNullOrEmpty(def.Type)) continue;
                if (!HasKey(keys, $"{channel}:{def.Type}"))
                    throw new InvalidOperationException(
                        $"Named provider '{name}' for channel '{channel}' uses type '{def.Type}' " +
                        $"but no credentials were found for it.");
                if (!string.IsNullOrEmpty(def.Fallback) && !HasKey(keys, $"{channel}:{def.Fallback}"))
                    throw new InvalidOperationException(
                        $"Named provider '{name}' for channel '{channel}' has fallback '{def.Fallback}' " +
                        $"but no credentials were found for it.");
            }
        }

        CheckProviders(options.Email?.Providers,    "email",    registeredKeys);
        CheckProviders(options.Sms?.Providers,      "sms",      registeredKeys);
        CheckProviders(options.Push?.Providers,     "push",     registeredKeys);
        CheckProviders(options.WhatsApp?.Providers, "whatsapp", registeredKeys);
    }
}
