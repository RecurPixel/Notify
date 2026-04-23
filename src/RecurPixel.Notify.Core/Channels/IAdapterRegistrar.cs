using Microsoft.Extensions.DependencyInjection;

namespace RecurPixel.Notify.Channels;

/// <summary>
/// Implemented by every adapter package to own its own DI registration logic.
/// Apply <see cref="ChannelAdapterAttribute"/> to each implementation to declare the
/// logical channel name and provider key used during auto-discovery.
/// </summary>
/// <remarks>
/// The Orchestrator's scanner discovers all types that implement
/// <see cref="IAdapterRegistrar"/> and are decorated with <see cref="ChannelAdapterAttribute"/>,
/// instantiates them, and calls <see cref="IsConfigured"/> to decide whether to register.
/// This replaces the previous monolithic switch/case tables in the Orchestrator.
/// </remarks>
public interface IAdapterRegistrar
{
    /// <summary>
    /// Returns <see langword="true"/> when the minimum required credentials for this adapter
    /// are present in <paramref name="options"/>. The Orchestrator calls this to filter out
    /// unconfigured adapters silently at startup.
    /// </summary>
    /// <param name="options">The resolved <see cref="NotifyOptions"/> POCO.</param>
    bool IsConfigured(NotifyOptions options);

    /// <summary>
    /// Registers everything the adapter needs into <paramref name="services"/>:
    /// <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>, named
    /// <see cref="System.Net.Http.HttpClient"/> (if required), vendor SDK clients
    /// (if required), and the keyed <see cref="INotificationChannel"/> singleton.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">The resolved <see cref="NotifyOptions"/> POCO.</param>
    void Register(IServiceCollection services, NotifyOptions options);
}
