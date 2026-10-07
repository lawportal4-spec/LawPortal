using LawPortal.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.MonthlyPrice).HasColumnType("decimal(10,2)");
        builder.Property(p => p.CommissionPercentageOverride).HasColumnType("decimal(5,2)");
    }
}

public class LawyerSubscriptionConfiguration : IEntityTypeConfiguration<LawyerSubscription>
{
    public void Configure(EntityTypeBuilder<LawyerSubscription> builder)
    {
        builder.ToTable("lawyer_subscriptions");
        builder.HasIndex(s => s.LawyerProfileId);
        builder.HasOne(s => s.Plan).WithMany().HasForeignKey(s => s.PlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(s => s.Invoices).WithOne(i => i.LawyerSubscription).HasForeignKey(i => i.LawyerSubscriptionId);
    }
}

public class SubscriptionInvoiceConfiguration : IEntityTypeConfiguration<SubscriptionInvoice>
{
    public void Configure(EntityTypeBuilder<SubscriptionInvoice> builder)
    {
        builder.ToTable("subscription_invoices");
        builder.HasIndex(i => i.Number).IsUnique();
        builder.HasIndex(i => i.GatewayPaymentId);
        builder.Property(i => i.SubtotalExVat).HasColumnType("decimal(10,2)");
        builder.Property(i => i.DiscountAmount).HasColumnType("decimal(10,2)");
        builder.Property(i => i.VatAmount).HasColumnType("decimal(10,2)");
        builder.Property(i => i.Total).HasColumnType("decimal(10,2)");
    }
}
