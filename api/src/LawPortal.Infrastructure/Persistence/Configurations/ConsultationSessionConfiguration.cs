using LawPortal.Domain.Calls;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class ConsultationSessionConfiguration : IEntityTypeConfiguration<ConsultationSession>
{
    public void Configure(EntityTypeBuilder<ConsultationSession> builder)
    {
        builder.ToTable("consultation_sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RoomName).HasMaxLength(100).IsRequired();
        builder.HasIndex(s => s.RoomName).IsUnique();
        builder.HasIndex(s => s.ServiceRequestId);
        builder.Property(s => s.RecordingStorageKey).HasMaxLength(500);
    }
}
