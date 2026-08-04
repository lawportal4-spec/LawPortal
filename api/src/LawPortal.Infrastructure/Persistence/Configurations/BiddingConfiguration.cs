using LawPortal.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class BiddingRequestConfiguration : IEntityTypeConfiguration<BiddingRequest>
{
    public void Configure(EntityTypeBuilder<BiddingRequest> builder)
    {
        builder.ToTable("bidding_requests");
        builder.HasMany(b => b.Invitations).WithOne().HasForeignKey(i => i.ServiceRequestId);
        builder.HasMany(b => b.Offers).WithOne().HasForeignKey(o => o.ServiceRequestId);
    }
}

public class RequestInvitationConfiguration : IEntityTypeConfiguration<RequestInvitation>
{
    public void Configure(EntityTypeBuilder<RequestInvitation> builder)
    {
        builder.ToTable("request_invitations");
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => new { i.ServiceRequestId, i.LawyerProfileId }).IsUnique();
        builder.HasIndex(i => i.LawyerProfileId);
    }
}

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("offers");
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => new { o.ServiceRequestId, o.LawyerProfileId }).IsUnique();
        builder.HasMany(o => o.Revisions).WithOne().HasForeignKey(r => r.OfferId);
    }
}

public class OfferRevisionConfiguration : IEntityTypeConfiguration<OfferRevision>
{
    public void Configure(EntityTypeBuilder<OfferRevision> builder)
    {
        builder.ToTable("offer_revisions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Amount).HasColumnType("decimal(10,2)");
        builder.Property(r => r.Message).HasMaxLength(1000);
        builder.HasIndex(r => new { r.OfferId, r.CreatedAtUtc });
    }
}
