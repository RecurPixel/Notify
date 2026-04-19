using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RecurPixel.Notify.Dashboard.EfCore;

/// <summary>
/// Extension methods for registering the EF Core notification log store.
/// </summary>
public static class DashboardEfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the EF Core notification log store using <see cref="NotifyDashboardDbContext"/>
    /// as a standalone context (Option A — own connection string, own migration).
    /// <para>
    /// The <paramref name="configure"/> action lets you configure the DbContext options
    /// without binding the library to any specific EF Core provider. Supply your provider
    /// in the action:
    /// <code>
    /// builder.Services.AddNotifyDashboardEfCore(options =>
    ///     options.UseSqlServer(connectionString));
    ///
    /// builder.Services.AddNotifyDashboardEfCore(options =>
    ///     options.UseNpgsql(connectionString));
    ///
    /// builder.Services.AddNotifyDashboardEfCore(options =>
    ///     options.UseSqlite(connectionString));
    /// </code>
    /// </para>
    /// <para>
    /// Also call <c>AddNotifyDashboard()</c> (from <c>RecurPixel.Notify.Dashboard</c>) to
    /// enable automatic log writes after every send attempt.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure <see cref="DbContextOptionsBuilder{NotifyDashboardDbContext}"/>.</param>
    public static IServiceCollection AddNotifyDashboardEfCore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configure)
    {
        services.AddDbContext<NotifyDashboardDbContext>(configure);

        // Register the EF Core store as the INotificationLogStore implementation.
        // TryAdd: a user-supplied store registered before this call wins.
        services.TryAddScoped<INotificationLogStore, EfCoreNotificationLogStore>();

        return services;
    }
}
