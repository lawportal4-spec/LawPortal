using System.Reflection;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Audit;
using LawPortal.Domain.Billing;
using LawPortal.Domain.Calls;
using LawPortal.Domain.Catalog;
using LawPortal.Domain.Chat;
using LawPortal.Domain.Common;
using LawPortal.Domain.Files;
using LawPortal.Domain.FreeMinutes;
using LawPortal.Domain.Identity;
using LawPortal.Domain.Ledger;
using LawPortal.Domain.Notifications;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Pricing;
using LawPortal.Domain.Requests;
using LawPortal.Domain.Reviews;
using LawPortal.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using WalletEntity = LawPortal.Domain.Wallet.Wallet;
using WalletTransactionEntity = LawPortal.Domain.Wallet.WalletTransaction;

namespace LawPortal.Infrastructure.Persistence;

public class LawPortalDbContext(DbContextOptions<LawPortalDbContext> options)
    : DbContext(options), ILawPortalDbContext
{
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<ExperienceRange> ExperienceRanges => Set<ExperienceRange>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<SubSpecialty> SubSpecialties => Set<SubSpecialty>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<ServiceCatalogItem> ServiceCatalogItems => Set<ServiceCatalogItem>();
    public DbSet<ServiceVariant> ServiceVariants => Set<ServiceVariant>();

    public DbSet<User> Users => Set<User>();
    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
    public DbSet<LawyerProfile> LawyerProfiles => Set<LawyerProfile>();
    public DbSet<LawyerLicense> LawyerLicenses => Set<LawyerLicense>();
    public DbSet<LawyerSpecialty> LawyerSpecialties => Set<LawyerSpecialty>();
    public DbSet<LawyerLanguage> LawyerLanguages => Set<LawyerLanguage>();
    public DbSet<LawyerQualification> LawyerQualifications => Set<LawyerQualification>();
    public DbSet<LawyerPricing> LawyerPricings => Set<LawyerPricing>();
    public DbSet<AdminProfile> AdminProfiles => Set<AdminProfile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ConsultationRequest> ConsultationRequests => Set<ConsultationRequest>();
    public DbSet<CatalogRequest> CatalogRequests => Set<CatalogRequest>();
    public DbSet<BiddingRequest> BiddingRequests => Set<BiddingRequest>();
    public DbSet<RequestStatusHistory> RequestStatusHistories => Set<RequestStatusHistory>();
    public DbSet<RequestAttachment> RequestAttachments => Set<RequestAttachment>();
    public DbSet<RequestInvitation> RequestInvitations => Set<RequestInvitation>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<OfferRevision> OfferRevisions => Set<OfferRevision>();

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<WalletEntity> Wallets => Set<WalletEntity>();
    public DbSet<WalletTransactionEntity> WalletTransactions => Set<WalletTransactionEntity>();
    public DbSet<CommissionPolicy> CommissionPolicies => Set<CommissionPolicy>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<MessageThread> MessageThreads => Set<MessageThread>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ThreadParticipant> ThreadParticipants => Set<ThreadParticipant>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<FreeMinutesEntitlement> FreeMinutesEntitlements => Set<FreeMinutesEntitlement>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<ConsultationSession> ConsultationSessions => Set<ConsultationSession>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<LawyerSubscription> LawyerSubscriptions => Set<LawyerSubscription>();
    public DbSet<SubscriptionInvoice> SubscriptionInvoices => Set<SubscriptionInvoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditableEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampAuditableEntities();
        return base.SaveChanges();
    }

    private void StampAuditableEntities()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                // Respect a pre-set value (e.g. the dev seeder backdating "joined" timestamps
                // for realistic demo variety) rather than always overwriting with "now".
                if (entry.Entity.CreatedAtUtc == default) entry.Entity.CreatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
