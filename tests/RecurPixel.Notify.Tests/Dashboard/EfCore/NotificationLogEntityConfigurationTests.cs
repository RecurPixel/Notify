using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using RecurPixel.Notify.Dashboard.EfCore;

namespace RecurPixel.Notify.Tests.Dashboard.EfCore;

public class NotificationLogEntityConfigurationTests
{
    private static IEntityType GetEntityType()
    {
        var options = new DbContextOptionsBuilder<NotifyDashboardDbContext>()
            .UseInMemoryDatabase("schema-test")
            .Options;
        using var db = new NotifyDashboardDbContext(options);
        return db.Model.FindEntityType(typeof(NotificationLog))!;
    }

    [Fact]
    public void EntityType_IsNotNull()
    {
        Assert.NotNull(GetEntityType());
    }

    [Theory]
    [InlineData(nameof(NotificationLog.Channel))]
    [InlineData(nameof(NotificationLog.Provider))]
    [InlineData(nameof(NotificationLog.Recipient))]
    [InlineData(nameof(NotificationLog.SentAt))]
    [InlineData(nameof(NotificationLog.Success))]
    [InlineData(nameof(NotificationLog.IsBulk))]
    [InlineData(nameof(NotificationLog.UsedFallback))]
    public void RequiredProperty_IsNotNullable(string propertyName)
    {
        var prop = GetEntityType().FindProperty(propertyName)!;
        Assert.False(prop.IsNullable, $"{propertyName} should not be nullable");
    }

    [Theory]
    [InlineData(nameof(NotificationLog.Subject))]
    [InlineData(nameof(NotificationLog.EventName))]
    [InlineData(nameof(NotificationLog.ProviderId))]
    [InlineData(nameof(NotificationLog.Error))]
    [InlineData(nameof(NotificationLog.BulkBatchId))]
    [InlineData(nameof(NotificationLog.NamedProvider))]
    public void NullableProperty_IsNullable(string propertyName)
    {
        var prop = GetEntityType().FindProperty(propertyName)!;
        Assert.True(prop.IsNullable, $"{propertyName} should be nullable");
    }

    [Theory]
    [InlineData(nameof(NotificationLog.Channel),   50)]
    [InlineData(nameof(NotificationLog.Provider),  50)]
    [InlineData(nameof(NotificationLog.Recipient), 500)]
    [InlineData(nameof(NotificationLog.Subject),   500)]
    [InlineData(nameof(NotificationLog.EventName), 200)]
    [InlineData(nameof(NotificationLog.ProviderId),200)]
    [InlineData(nameof(NotificationLog.BulkBatchId), 50)]
    [InlineData(nameof(NotificationLog.NamedProvider), 100)]
    [InlineData(nameof(NotificationLog.Error),    2000)]
    public void StringProperty_HasCorrectMaxLength(string propertyName, int expectedMaxLength)
    {
        var prop = GetEntityType().FindProperty(propertyName)!;
        Assert.Equal(expectedMaxLength, prop.GetMaxLength());
    }

    [Fact]
    public void Id_IsValueGeneratedOnAdd()
    {
        var prop = GetEntityType().FindProperty(nameof(NotificationLog.Id))!;
        Assert.Equal(ValueGenerated.OnAdd, prop.ValueGenerated);
    }

    [Fact]
    public void ModelBuilderExtension_AppliesConfiguration()
    {
        var builder = new ModelBuilder();
        builder.AddNotifyDashboard();

        var entityType = builder.Model.FindEntityType(typeof(NotificationLog));
        Assert.NotNull(entityType);
    }
}
