using LawPortal.Domain.Audit;
using LawPortal.Domain.Billing;
using LawPortal.Domain.Calls;
using LawPortal.Domain.Catalog;
using LawPortal.Domain.Chat;
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

namespace LawPortal.Application.Common.Interfaces;

/// <summary>
/// The Application layer's view of the persistence store — implemented by
/// LawPortal.Infrastructure.Persistence.LawPortalDbContext. Keeps Application free of any
/// EF Core / Pomelo package dependency.
/// </summary>
public interface ILawPortalDbContext
{
    DbSet<Region> Regions { get; }
    DbSet<City> Cities { get; }
    DbSet<Language> Languages { get; }
    DbSet<ExperienceRange> ExperienceRanges { get; }
    DbSet<Specialty> Specialties { get; }
    DbSet<SubSpecialty> SubSpecialties { get; }
    DbSet<ServiceCategory> ServiceCategories { get; }
    DbSet<ServiceCatalogItem> ServiceCatalogItems { get; }
    DbSet<ServiceVariant> ServiceVariants { get; }

    DbSet<User> Users { get; }
    DbSet<ClientProfile> ClientProfiles { get; }
    DbSet<LawyerProfile> LawyerProfiles { get; }
    DbSet<LawyerLicense> LawyerLicenses { get; }
    DbSet<LawyerSpecialty> LawyerSpecialties { get; }
    DbSet<LawyerLanguage> LawyerLanguages { get; }
    DbSet<LawyerQualification> LawyerQualifications { get; }
    DbSet<LawyerPricing> LawyerPricings { get; }
    DbSet<AdminProfile> AdminProfiles { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<EmailVerificationToken> EmailVerificationTokens { get; }
    DbSet<LawyerContactNumber> LawyerContactNumbers { get; }
    DbSet<OtpChallenge> OtpChallenges { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DbSet<ServiceRequest> ServiceRequests { get; }
    DbSet<ConsultationRequest> ConsultationRequests { get; }
    DbSet<CatalogRequest> CatalogRequests { get; }
    DbSet<BiddingRequest> BiddingRequests { get; }
    DbSet<RequestStatusHistory> RequestStatusHistories { get; }
    DbSet<RequestAttachment> RequestAttachments { get; }
    DbSet<RequestInvitation> RequestInvitations { get; }
    DbSet<Offer> Offers { get; }
    DbSet<OfferRevision> OfferRevisions { get; }

    DbSet<Payment> Payments { get; }
    DbSet<Refund> Refunds { get; }
    DbSet<Payout> Payouts { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<WalletEntity> Wallets { get; }
    DbSet<WalletTransactionEntity> WalletTransactions { get; }
    DbSet<CommissionPolicy> CommissionPolicies { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<DiscountCode> DiscountCodes { get; }
    DbSet<DiscountRedemption> DiscountRedemptions { get; }

    DbSet<MessageThread> MessageThreads { get; }
    DbSet<Message> Messages { get; }
    DbSet<ThreadParticipant> ThreadParticipants { get; }
    DbSet<Report> Reports { get; }
    DbSet<Block> Blocks { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<FreeMinutesEntitlement> FreeMinutesEntitlements { get; }

    DbSet<Review> Reviews { get; }

    DbSet<ConsultationSession> ConsultationSessions { get; }

    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<LawyerSubscription> LawyerSubscriptions { get; }
    DbSet<SubscriptionInvoice> SubscriptionInvoices { get; }
    DbSet<RegistrationFeeSetting> RegistrationFeeSettings { get; }
    DbSet<LawyerRegistrationFeeInvoice> RegistrationFeeInvoices { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
