using Microsoft.EntityFrameworkCore;

namespace RecurPixel.Notify.Dashboard.EfCore;

/// <summary>
/// Extension methods for plugging the notification log schema into an existing DbContext.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies the <see cref="NotificationLog"/> entity configuration to the model builder.
    /// Call this from <c>OnModelCreating</c> in your existing <c>DbContext</c> to add the
    /// <c>NotificationLogs</c> table to your existing schema and migration (Option B).
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder builder)
    /// {
    ///     base.OnModelCreating(builder);
    ///     builder.AddNotifyDashboard();
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="modelBuilder">The model builder from <c>OnModelCreating</c>.</param>
    public static ModelBuilder AddNotifyDashboard(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new NotificationLogEntityTypeConfiguration());
        return modelBuilder;
    }
}
