using Microsoft.EntityFrameworkCore;

namespace RecurPixel.Notify.Dashboard.EfCore;

/// <summary>
/// Standalone EF Core DbContext for the RecurPixel.Notify dashboard.
/// Use this when you want the notification log in its own database or migration set
/// (Option A — standalone context).
/// <para>
/// If you prefer to add the <see cref="NotificationLog"/> table to your existing DbContext,
/// call <see cref="ModelBuilderExtensions.AddNotifyDashboard"/> in your
/// <c>OnModelCreating</c> override instead (Option B — plug into existing context).
/// </para>
/// <para>
/// To create a migration for the standalone context:
/// <code>dotnet ef migrations add InitNotifyDashboard --context NotifyDashboardDbContext</code>
/// </para>
/// </summary>
public class NotifyDashboardDbContext : DbContext
{
    /// <summary>Initialises the context with the provided options.</summary>
    public NotifyDashboardDbContext(DbContextOptions<NotifyDashboardDbContext> options)
        : base(options) { }

    /// <summary>The notification log table.</summary>
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddNotifyDashboard();
    }
}
