using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using LawPortal.Domain.Ledger;
using LawPortal.Domain.Subscriptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Dashboard.Queries;

public record RequestStatusCountDto(string Status, int Count);
public record SubscriptionPlanCountDto(string PlanNameEn, int Count);

public record AdminDashboardDto(
    int TotalClients,
    int TotalVerifiedLawyers,
    int PendingLawyerVerifications,
    int TotalAdmins,
    IReadOnlyList<RequestStatusCountDto> RequestsByStatus,
    decimal CommissionRevenueThisMonth,
    decimal SubscriptionRevenueThisMonth,
    decimal RegistrationFeeRevenueThisMonth,
    bool LedgerIsBalanced,
    IReadOnlyList<SubscriptionPlanCountDto> ActiveSubscriptionsByPlan);

/// <summary>One aggregate read for the backoffice landing page — every number here is a real
/// query against live data, not a placeholder metric; <see cref="AdminDashboardDto.LedgerIsBalanced"/>
/// is the same trial-balance check <c>GetLedgerSummaryQuery</c> runs in full, surfaced here as a
/// single at-a-glance health signal.</summary>
public record GetAdminDashboardQuery : IRequest<AdminDashboardDto>;

public class GetAdminDashboardHandler(ILawPortalDbContext db) : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        // The month starts at midnight Riyadh time (UTC+3, no DST) — the same boundary as the ledger's «هذا الشهر».
        var riyadhNow = now.AddHours(3);
        var monthStart = new DateTime(riyadhNow.Year, riyadhNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(-3);

        var totalClients = await db.Users.CountAsync(u => u.UserType == UserType.Client, cancellationToken);
        var totalVerifiedLawyers = await db.LawyerProfiles.CountAsync(l => l.IsVerified, cancellationToken);
        var pendingVerifications = await db.LawyerProfiles
            // Unactivated sign-ups aren't applicants yet — same rule as the registrations list.
            .CountAsync(l => l.License!.VerificationStatus == LicenseVerificationStatus.PendingReview
                && (l.User!.PhoneE164 == null || l.User.IsPhoneVerified), cancellationToken);
        var totalAdmins = await db.Users.CountAsync(u => u.UserType == UserType.Admin, cancellationToken);

        var requestsByStatus = await db.ServiceRequests
            .GroupBy(r => r.Status)
            .Select(g => new RequestStatusCountDto(g.Key.ToString(), g.Count()))
            .ToListAsync(cancellationToken);

        // Net of reversals (refunds debit the revenue account), so it matches the ledger page.
        var commissionThisMonth = await db.LedgerEntries
            .Where(e => e.Account == LedgerAccount.CommissionRevenue && e.CreatedAtUtc >= monthStart)
            .SumAsync(e => e.IsDebit ? -e.Amount : e.Amount, cancellationToken);
        var subscriptionRevenueThisMonth = await db.LedgerEntries
            .Where(e => e.Account == LedgerAccount.SubscriptionRevenue && e.CreatedAtUtc >= monthStart)
            .SumAsync(e => e.IsDebit ? -e.Amount : e.Amount, cancellationToken);
        var registrationFeeRevenueThisMonth = await db.LedgerEntries
            .Where(e => e.Account == LedgerAccount.RegistrationFeeRevenue && e.CreatedAtUtc >= monthStart)
            .SumAsync(e => e.IsDebit ? -e.Amount : e.Amount, cancellationToken);

        var totalDebits = await db.LedgerEntries.Where(e => e.IsDebit).SumAsync(e => e.Amount, cancellationToken);
        var totalCredits = await db.LedgerEntries.Where(e => !e.IsDebit).SumAsync(e => e.Amount, cancellationToken);

        var activeSubscriptions = await db.LawyerSubscriptions
            .Where(s => s.CurrentPeriodEndUtc > now && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Canceled))
            .Include(s => s.Plan)
            .GroupBy(s => s.Plan!.NameEn)
            .Select(g => new SubscriptionPlanCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        return new AdminDashboardDto(
            totalClients, totalVerifiedLawyers, pendingVerifications, totalAdmins,
            requestsByStatus, commissionThisMonth, subscriptionRevenueThisMonth, registrationFeeRevenueThisMonth, totalDebits == totalCredits,
            activeSubscriptions);
    }
}
