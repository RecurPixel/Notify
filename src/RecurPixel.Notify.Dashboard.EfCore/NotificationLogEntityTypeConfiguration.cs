using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RecurPixel.Notify.Dashboard.EfCore;

/// <summary>
/// EF Core entity type configuration for <see cref="NotificationLog"/>.
/// Applied automatically by <see cref="NotifyDashboardDbContext"/> and by
/// <see cref="ModelBuilderExtensions.AddNotifyDashboard"/> when plugging into an existing DbContext.
/// </summary>
public sealed class NotificationLogEntityTypeConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedOnAdd();

        builder.Property(l => l.Channel)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Recipient)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(l => l.Subject)
            .HasMaxLength(500);

        builder.Property(l => l.EventName)
            .HasMaxLength(200);

        builder.Property(l => l.ProviderId)
            .HasMaxLength(200);

        builder.Property(l => l.Error)
            .HasMaxLength(2000);

        builder.Property(l => l.BulkBatchId)
            .HasMaxLength(50);

        builder.Property(l => l.NamedProvider)
            .HasMaxLength(100);

        builder.Property(l => l.SentAt)
            .IsRequired();

        // Index for time-range queries (most common dashboard filter)
        builder.HasIndex(l => l.SentAt)
            .HasDatabaseName("IX_NotificationLogs_SentAt");

        // Index for batch retrieval
        builder.HasIndex(l => l.BulkBatchId)
            .HasDatabaseName("IX_NotificationLogs_BulkBatchId");

        // Composite index for the most common filtered dashboard query
        builder.HasIndex(l => new { l.Channel, l.Success, l.SentAt })
            .HasDatabaseName("IX_NotificationLogs_Channel_Success_SentAt");
    }
}
