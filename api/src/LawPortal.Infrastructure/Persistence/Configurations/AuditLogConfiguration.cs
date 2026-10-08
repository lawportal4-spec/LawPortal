using LawPortal.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.ActorRole).HasMaxLength(50);
        builder.Property(a => a.EntityType).HasMaxLength(100);
        builder.Property(a => a.EntityId).HasMaxLength(100);
        builder.Property(a => a.Details).HasColumnType("text");
        builder.HasIndex(a => a.OccurredAtUtc);
    }
}

public class AdminNoteConfiguration : IEntityTypeConfiguration<LawPortal.Domain.Audit.AdminNote>
{
    public void Configure(EntityTypeBuilder<LawPortal.Domain.Audit.AdminNote> builder)
    {
        builder.ToTable("admin_notes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.EntityType).HasMaxLength(20);
        builder.Property(n => n.Body).HasMaxLength(2000);
        builder.Property(n => n.AuthorName).HasMaxLength(200);
        builder.HasIndex(n => new { n.EntityType, n.EntityId });
    }
}
