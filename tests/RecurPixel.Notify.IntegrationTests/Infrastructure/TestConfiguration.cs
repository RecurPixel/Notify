using Microsoft.Extensions.Configuration;

namespace RecurPixel.Notify.IntegrationTests.Infrastructure;

/// <summary>
/// Loads appsettings.integration.json once per test run.
/// All test classes read credentials from this shared config.
/// </summary>
internal static class TestConfiguration
{
    private static readonly Lazy<IConfiguration> _instance = new(BuildConfiguration);

    /// <summary>The loaded integration test configuration.</summary>
    public static IConfiguration Instance => _instance.Value;

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.integration.json", optional: false, reloadOnChange: false)
            .Build();
    }

    // ── Convenience readers ───────────────────────────────────────────

    /// <summary>Returns the config value at <paramref name="key"/>, or empty string if missing.</summary>
    public static string Get(string key) =>
        Instance[key] ?? string.Empty;

    /// <summary>Returns true if the config value at <paramref name="key"/> is non-empty.</summary>
    public static bool Has(string key) =>
        !string.IsNullOrWhiteSpace(Instance[key]);

    /// <summary>Returns true if ALL of the specified keys have non-empty values.</summary>
    public static bool HasAll(params string[] keys) =>
        keys.All(Has);
}
