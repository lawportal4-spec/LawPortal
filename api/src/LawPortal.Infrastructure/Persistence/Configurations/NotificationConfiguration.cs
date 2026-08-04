using LawPortal.Domain.FreeMinutes;
using LawPortal.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(1000).IsRequired();
        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}

public class FreeMinutesEntitlementConfiguration : IEntityTypeConfiguration<FreeMinutesEntitlement>
{
    public void Configure(EntityTypeBuilder<FreeMinutesEntitlement> builder)
    {
        builder.ToTable("free_minutes_entitlements");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.ClientId).IsUnique();
    }
}
