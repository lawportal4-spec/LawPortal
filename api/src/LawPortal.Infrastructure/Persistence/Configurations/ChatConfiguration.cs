using LawPortal.Domain.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class MessageThreadConfiguration : IEntityTypeConfiguration<MessageThread>
{
    public void Configure(EntityTypeBuilder<MessageThread> builder)
    {
        builder.ToTable("message_threads");
        builder.HasKey(t => t.Id);
        builder.HasOne(t => t.ServiceRequest).WithMany().HasForeignKey(t => t.ServiceRequestId);
        builder.HasIndex(t => t.ServiceRequestId).IsUnique();
        builder.HasMany(t => t.Messages).WithOne(m => m.Thread!).HasForeignKey(m => m.ThreadId);
        builder.HasMany(t => t.Participants).WithOne(p => p.Thread!).HasForeignKey(p => p.ThreadId);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Body).HasColumnType("text").IsRequired();
        builder.HasIndex(m => new { m.ThreadId, m.SentAtUtc });
    }
}

public class ThreadParticipantConfiguration : IEntityTypeConfiguration<ThreadParticipant>
{
    public void Configure(EntityTypeBuilder<ThreadParticipant> builder)
    {
        builder.ToTable("thread_participants");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.ThreadId, p.UserId }).IsUnique();
    }
}

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Details).HasMaxLength(1000);
        builder.HasIndex(r => r.ReportedUserId);
    }
}

public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.ToTable("blocks");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.BlockerUserId, b.BlockedUserId }).IsUnique();
    }
}
