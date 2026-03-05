using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Core.Channels;
using RecurPixel.Notify.Core.Extensions;
using RecurPixel.Notify.Core.Options;
using RecurPixel.Notify.Core.Options.Providers;
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

        // Ensure IHttpClientFactory is available — required by any HTTP-based channel adapter.
        // Idempotent: safe to call multiple times.
        services.AddHttpClient();

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
                "sendgrid" => !string.IsNullOrEmpty(options.Email?.SendGrid?.ApiKey),
                "smtp" => !string.IsNullOrEmpty(options.Email?.Smtp?.Host),
                "mailgun" => !string.IsNullOrEmpty(options.Email?.Mailgun?.ApiKey),
                "resend" => !string.IsNullOrEmpty(options.Email?.Resend?.ApiKey),
                "postmark" => !string.IsNullOrEmpty(options.Email?.Postmark?.ApiKey),
                "awsses" => !string.IsNullOrEmpty(options.Email?.AwsSes?.AccessKey),
                "azurecommemail" => !string.IsNullOrEmpty(options.Email?.AzureCommEmail?.ConnectionString),
                _ => false
            },
            "sms" => provider switch
            {
                "twilio" => !string.IsNullOrEmpty(options.Sms?.Twilio?.AccountSid),
                "vonage" => !string.IsNullOrEmpty(options.Sms?.Vonage?.ApiKey),
                "plivo" => !string.IsNullOrEmpty(options.Sms?.Plivo?.AuthId),
                "sinch" => !string.IsNullOrEmpty(options.Sms?.Sinch?.ServicePlanId),
                "messagebird" => !string.IsNullOrEmpty(options.Sms?.MessageBird?.ApiKey),
                "awssns" => !string.IsNullOrEmpty(options.Sms?.AwsSns?.AccessKey),
                "azurecommsms" => !string.IsNullOrEmpty(options.Sms?.AzureCommSms?.ConnectionString),
                _ => false
            },
            "push" => provider switch
            {
                "fcm" => !string.IsNullOrEmpty(options.Push?.Fcm?.ProjectId),
                "apns" => !string.IsNullOrEmpty(options.Push?.Apns?.KeyId),
                "onesignal" => !string.IsNullOrEmpty(options.Push?.OneSignal?.AppId),
                "expo" => options.Push?.Expo is not null,
                _ => false
            },
            "whatsapp" => provider switch
            {
                "twilio" => !string.IsNullOrEmpty(options.WhatsApp?.Twilio?.AccountSid),
                "metacloud" => !string.IsNullOrEmpty(options.WhatsApp?.MetaCloud?.AccessToken),
                "vonage" => !string.IsNullOrEmpty(options.WhatsApp?.Vonage?.ApiKey),
                _ => false
            },
            "slack" => !string.IsNullOrEmpty(options.Slack?.WebhookUrl) || !string.IsNullOrEmpty(options.Slack?.BotToken),
            "discord" => !string.IsNullOrEmpty(options.Discord?.WebhookUrl),
            "teams" => !string.IsNullOrEmpty(options.Teams?.WebhookUrl),
            "telegram" => !string.IsNullOrEmpty(options.Telegram?.BotToken),
            "facebook" => !string.IsNullOrEmpty(options.Facebook?.PageAccessToken),
            "line" => !string.IsNullOrEmpty(options.Line?.ChannelAccessToken),
            "viber" => !string.IsNullOrEmpty(options.Viber?.BotAuthToken),
            "mattermost" => !string.IsNullOrEmpty(options.Mattermost?.WebhookUrl),
            "rocketchat" => !string.IsNullOrEmpty(options.RocketChat?.WebhookUrl),
            "inapp" => true,  // Always registered; handler wired by AddInAppChannel / OnDeliver
            _ => false
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
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
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

        // Configure IOptions<T> for every known adapter options type so that adapters
        // constructed by DI receive the real credentials, not empty defaults.
        ConfigureAllKnownOptions(services, options);

        var registered = new List<string>();

        foreach (var type in DiscoverAdapters())
        {
            var attr = type.GetCustomAttribute<ChannelAdapterAttribute>()!;
            var key = $"{attr.Channel}:{attr.Provider}";

            if (!IsAdapterConfigured(options, attr.Channel, attr.Provider))
                continue;

            services.TryAddKeyedSingleton(typeof(INotificationChannel), key, type);
            registered.Add(key);
        }

        return registered;
    }

    /// <summary>
    /// Configures <see cref="IOptions{T}"/> for every known adapter options type from the
    /// <paramref name="notifyOptions"/> POCO. Called once during auto-registration so that
    /// adapters constructed by DI receive real credentials rather than empty defaults.
    /// Configuring options for adapters whose packages are not installed is harmless.
    /// </summary>
    private static void ConfigureAllKnownOptions(IServiceCollection services, NotifyOptions notifyOptions)
    {
        // ── Email ─────────────────────────────────────────────────────────────
        if (notifyOptions.Email?.SendGrid is { } sg)
            services.Configure<SendGridOptions>(o =>
            {
                o.ApiKey = sg.ApiKey;
                o.FromEmail = sg.FromEmail;
                o.FromName = sg.FromName;
            });

        if (notifyOptions.Email?.Smtp is { } smtp)
            services.Configure<SmtpOptions>(o =>
            {
                o.Host = smtp.Host;
                o.Port = smtp.Port;
                o.Username = smtp.Username;
                o.Password = smtp.Password;
                o.UseSsl = smtp.UseSsl;
                o.FromEmail = smtp.FromEmail;
                o.FromName = smtp.FromName;
            });

        if (notifyOptions.Email?.Mailgun is { } mg)
            services.Configure<MailgunOptions>(o =>
            {
                o.ApiKey = mg.ApiKey;
                o.Domain = mg.Domain;
                o.FromEmail = mg.FromEmail;
                o.FromName = mg.FromName;
            });

        if (notifyOptions.Email?.Resend is { } rs)
            services.Configure<ResendOptions>(o =>
            {
                o.ApiKey = rs.ApiKey;
                o.FromEmail = rs.FromEmail;
                o.FromName = rs.FromName;
            });

        if (notifyOptions.Email?.Postmark is { } pm)
            services.Configure<PostmarkOptions>(o =>
            {
                o.ApiKey = pm.ApiKey;
                o.FromEmail = pm.FromEmail;
                o.FromName = pm.FromName;
            });

        if (notifyOptions.Email?.AwsSes is { } ses)
            services.Configure<AwsSesOptions>(o =>
            {
                o.AccessKey = ses.AccessKey;
                o.SecretKey = ses.SecretKey;
                o.Region = ses.Region;
                o.FromEmail = ses.FromEmail;
                o.FromName = ses.FromName;
            });

        if (notifyOptions.Email?.AzureCommEmail is { } ace)
            services.Configure<AzureCommEmailOptions>(o =>
            {
                o.ConnectionString = ace.ConnectionString;
                o.FromEmail = ace.FromEmail;
                o.FromName = ace.FromName;
            });

        // ── SMS ───────────────────────────────────────────────────────────────
        if (notifyOptions.Sms?.Twilio is { } st)
            services.Configure<TwilioOptions>(o =>
            {
                o.AccountSid = st.AccountSid;
                o.AuthToken = st.AuthToken;
                o.FromNumber = st.FromNumber;
            });

        if (notifyOptions.Sms?.Vonage is { } sv)
            services.Configure<VonageOptions>(o =>
            {
                o.ApiKey = sv.ApiKey;
                o.ApiSecret = sv.ApiSecret;
                o.FromNumber = sv.FromNumber;
            });

        if (notifyOptions.Sms?.Plivo is { } pl)
            services.Configure<PlivoOptions>(o =>
            {
                o.AuthId = pl.AuthId;
                o.AuthToken = pl.AuthToken;
                o.FromNumber = pl.FromNumber;
            });

        if (notifyOptions.Sms?.Sinch is { } si)
            services.Configure<SinchOptions>(o =>
            {
                o.ServicePlanId = si.ServicePlanId;
                o.ApiToken = si.ApiToken;
                o.FromNumber = si.FromNumber;
            });

        if (notifyOptions.Sms?.MessageBird is { } mb)
            services.Configure<MessageBirdOptions>(o =>
            {
                o.ApiKey = mb.ApiKey;
                o.Originator = mb.Originator;
            });

        if (notifyOptions.Sms?.AwsSns is { } sns)
            services.Configure<AwsSnsOptions>(o =>
            {
                o.AccessKey = sns.AccessKey;
                o.SecretKey = sns.SecretKey;
                o.Region = sns.Region;
                o.SmsType = sns.SmsType;
                o.SenderId = sns.SenderId;
            });

        if (notifyOptions.Sms?.AzureCommSms is { } acs)
            services.Configure<AzureCommSmsOptions>(o =>
            {
                o.ConnectionString = acs.ConnectionString;
                o.FromNumber = acs.FromNumber;
            });

        // ── Push ──────────────────────────────────────────────────────────────
        if (notifyOptions.Push?.Fcm is { } fcm)
            services.Configure<FcmOptions>(o =>
            {
                o.ProjectId = fcm.ProjectId;
                o.ServiceAccountJson = fcm.ServiceAccountJson;
            });

        if (notifyOptions.Push?.Apns is { } apns)
            services.Configure<ApnsOptions>(o =>
            {
                o.KeyId = apns.KeyId;
                o.TeamId = apns.TeamId;
                o.BundleId = apns.BundleId;
                o.PrivateKey = apns.PrivateKey;
            });

        if (notifyOptions.Push?.OneSignal is { } os)
            services.Configure<OneSignalOptions>(o =>
            {
                o.AppId = os.AppId;
                o.ApiKey = os.ApiKey;
            });

        if (notifyOptions.Push?.Expo is { } expo)
            services.Configure<ExpoOptions>(o => { o.AccessToken = expo.AccessToken; });

        // ── WhatsApp ──────────────────────────────────────────────────────────
        // Note: WhatsApp-Twilio uses the same TwilioOptions class as SMS-Twilio.
        // If both channels are configured with Twilio, WhatsApp credentials win
        // for IOptions<TwilioOptions>. Adapters that need channel-specific
        // discrimination should use named options in a future release.
        if (notifyOptions.WhatsApp?.Twilio is { } wt)
            services.Configure<TwilioOptions>(o =>
            {
                o.AccountSid = wt.AccountSid;
                o.AuthToken = wt.AuthToken;
                o.FromNumber = wt.FromNumber;
            });

        if (notifyOptions.WhatsApp?.MetaCloud is { } mc)
            services.Configure<MetaCloudOptions>(o =>
            {
                o.AccessToken = mc.AccessToken;
                o.PhoneNumberId = mc.PhoneNumberId;
            });

        if (notifyOptions.WhatsApp?.Vonage is { } wv)
            services.Configure<VonageWhatsAppOptions>(o =>
            {
                o.ApiKey = wv.ApiKey;
                o.ApiSecret = wv.ApiSecret;
                o.FromNumber = wv.FromNumber;
            });

        // ── Messaging channels ────────────────────────────────────────────────
        if (notifyOptions.Slack is { } slack)
            services.Configure<SlackOptions>(o =>
            {
                o.WebhookUrl = slack.WebhookUrl;
                o.BotToken = slack.BotToken;
            });

        if (notifyOptions.Discord is { } discord)
            services.Configure<DiscordOptions>(o => { o.WebhookUrl = discord.WebhookUrl; });

        if (notifyOptions.Teams is { } teams)
            services.Configure<TeamsOptions>(o => { o.WebhookUrl = teams.WebhookUrl; });

        if (notifyOptions.Telegram is { } tg)
            services.Configure<TelegramOptions>(o =>
            {
                o.BotToken = tg.BotToken;
                o.ChatId = tg.ChatId;
                o.ParseMode = tg.ParseMode;
            });

        if (notifyOptions.Facebook is { } fb)
            services.Configure<FacebookOptions>(o => { o.PageAccessToken = fb.PageAccessToken; });

        if (notifyOptions.Line is { } line)
            services.Configure<LineOptions>(o => { o.ChannelAccessToken = line.ChannelAccessToken; });

        if (notifyOptions.Viber is { } viber)
            services.Configure<ViberOptions>(o =>
            {
                o.BotAuthToken = viber.BotAuthToken;
                o.SenderName = viber.SenderName;
                o.SenderAvatarUrl = viber.SenderAvatarUrl;
            });

        if (notifyOptions.Mattermost is { } mm)
            services.Configure<MattermostOptions>(o =>
            {
                o.WebhookUrl = mm.WebhookUrl;
                o.Username = mm.Username;
                o.Channel = mm.Channel;
            });

        if (notifyOptions.RocketChat is { } rc)
            services.Configure<RocketChatOptions>(o =>
            {
                o.WebhookUrl = rc.WebhookUrl;
                o.Username = rc.Username;
                o.Channel = rc.Channel;
            });
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

        Check(options.Email?.Provider, "email", "Email", registeredKeys);
        Check(options.Sms?.Provider, "sms", "Sms", registeredKeys);
        Check(options.Push?.Provider, "push", "Push", registeredKeys);
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

        CheckProviders(options.Email?.Providers, "email", registeredKeys);
        CheckProviders(options.Sms?.Providers, "sms", registeredKeys);
        CheckProviders(options.Push?.Providers, "push", registeredKeys);
        CheckProviders(options.WhatsApp?.Providers, "whatsapp", registeredKeys);
    }
}
