using LawPortal.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

/// <summary>Table-per-type: one narrow table per pricing-model subtype, sharing the base
/// "service_requests" table for everything common. Keeps list/dashboard queries (which only
/// ever need the base columns) off the mode-specific columns entirely.</summary>
public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("service_requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Number).HasMaxLength(30).IsRequired();
        builder.HasIndex(r => r.Number).IsUnique();
        builder.Property(r => r.Title).HasMaxLength(300);
        builder.Property(r => r.Description).HasColumnType("text");
        builder.Property(r => r.CurrencyCode).HasMaxLength(3);
        builder.Property(r => r.Subtotal).HasColumnType("decimal(10,2)");
        builder.Property(r => r.CancelReason).HasMaxLength(500);

        builder.HasOne(r => r.Client).WithMany().HasForeignKey(r => r.ClientId);
        builder.HasOne(r => r.Service).WithMany().HasForeignKey(r => r.ServiceId);
        builder.HasOne(r => r.Specialty).WithMany().HasForeignKey(r => r.SpecialtyId);
        builder.HasOne(r => r.SubSpecialty).WithMany().HasForeignKey(r => r.SubSpecialtyId);

        builder.HasMany(r => r.Attachments).WithOne(a => a.ServiceRequest!).HasForeignKey(a => a.ServiceRequestId);
        builder.HasMany(r => r.StatusHistory).WithOne().HasForeignKey(h => h.ServiceRequestId);

        builder.HasIndex(r => new { r.ClientId, r.Status });
    }
}

public class ConsultationRequestConfiguration : IEntityTypeConfiguration<ConsultationRequest>
{
    public void Configure(EntityTypeBuilder<ConsultationRequest> builder)
    {
        builder.ToTable("consultation_requests");
        builder.Property(c => c.VoiceNoteStorageKey).HasMaxLength(300);
        builder.HasOne(c => c.LawyerProfile).WithMany().HasForeignKey(c => c.LawyerProfileId);
    }
}

public class CatalogRequestConfiguration : IEntityTypeConfiguration<CatalogRequest>
{
    public void Configure(EntityTypeBuilder<CatalogRequest> builder)
    {
        builder.ToTable("catalog_requests");
        builder.Property(c => c.UnitPriceSnapshot).HasColumnType("decimal(10,2)");
        builder.HasOne(c => c.ServiceVariant).WithMany().HasForeignKey(c => c.ServiceVariantId);
    }
}

public class RequestStatusHistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<RequestStatusHistory> builder)
    {
        builder.ToTable("request_status_history");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Trigger).HasMaxLength(100);
    }
}
