using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

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

        services.AddNotifyOptions(notifyOptions);

        // Ensure IHttpClientFactory is available — required by any HTTP-based channel adapter.
        // Idempotent: safe to call multiple times.
        services.AddHttpClient();

        var registeredKeys = RegisterAdapters(services, notifyOptions);
        ValidateActiveProviders(notifyOptions, registeredKeys);

        services.AddRecurPixelNotifyOrchestrator(configureOrchestrator);

        return services;
    }

    // ── Orchestrator-only setup ───────────────────────────────────────────────

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

        services.AddSingleton(options);
        services.AddSingleton(options.Registry);

        services.TryAddSingleton<IOptions<NotifyOptions>>(sp =>
            Microsoft.Extensions.Options.Options.Create(sp.GetRequiredService<NotifyOptions>()));

        services.AddScoped<ChannelDispatcher>();
        services.AddScoped<INotifyService, NotifyService>();

        return services;
    }

    // ── Adapter scanner ───────────────────────────────────────────────────────

    /// <summary>
    /// Scans all loaded assemblies for types that implement <see cref="IAdapterRegistrar"/>
    /// and are decorated with <see cref="ChannelAdapterAttribute"/>.
    /// Guards against <see cref="ReflectionTypeLoadException"/> from partially loaded assemblies.
    /// </summary>
    private static IEnumerable<Type> DiscoverRegistrars()
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
                if (!typeof(IAdapterRegistrar).IsAssignableFrom(type)) continue;
                if (type.GetCustomAttribute<ChannelAdapterAttribute>() is null) continue;
                yield return type;
            }
        }
    }

    /// <summary>
    /// Loads all <c>RecurPixel.Notify.*.dll</c> assemblies found in the application base
    /// directory that are not already loaded, so adapter registrars are discoverable.
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

    /// <summary>
    /// Discovers all registrars, calls <see cref="IAdapterRegistrar.IsConfigured"/> to filter,
    /// then calls <see cref="IAdapterRegistrar.Register"/> for each passing adapter.
    /// Returns the list of registered <c>channel:provider</c> keys for startup validation.
    /// </summary>
    private static List<string> RegisterAdapters(IServiceCollection services, NotifyOptions options)
    {
        EnsureAdapterAssembliesLoaded();
        var registered = new List<string>();

        foreach (var registrarType in DiscoverRegistrars())
        {
            var registrar = (IAdapterRegistrar)Activator.CreateInstance(registrarType)!;
            var attr = registrarType.GetCustomAttribute<ChannelAdapterAttribute>()!;
            var key = $"{attr.Channel}:{attr.Provider}";

            if (!registrar.IsConfigured(options)) continue;

            registrar.Register(services, options);
            registered.Add(key);
        }

        return registered;
    }

    // ── Startup validation ────────────────────────────────────────────────────

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
