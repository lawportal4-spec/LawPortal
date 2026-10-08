using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Billing;
using LawPortal.Domain.Identity;
using LawPortal.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Directory;

/// <summary>One summary card. Labels live in i18n (<c>pageStats.{page}.{key}</c>); <see cref="Tone"/> is gold / success / danger or null.</summary>
public record PageStatDto(string Key, decimal Value, bool IsMoney, string? Tone = null);

/// <summary>The four cards at the top of an admin list page.</summary>
public record GetAdminPageStatsQuery(string Page) : IRequest<IReadOnlyList<PageStatDto>>;

public class GetAdminPageStatsHandler(ILawPortalDbContext db) : IRequestHandler<GetAdminPageStatsQuery, IReadOnlyList<PageStatDto>>
{
    public async Task<IReadOnlyList<PageStatDto>> Handle(GetAdminPageStatsQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var dayStart = now.Date;
        var weekAgo = now.AddDays(-7);

        switch (request.Page)
        {
            case "lawyers":
            {
                var soon = DateOnly.FromDateTime(now.AddDays(30));
                var today = DateOnly.FromDateTime(now);
                return
                [
                    new("total", await db.LawyerProfiles.CountAsync(ct), false),
                    new("pending", await db.LawyerLicenses.CountAsync(l => l.VerificationStatus == LicenseVerificationStatus.PendingReview, ct), false,
                        "gold"),
                    new("active", await db.LawyerProfiles.CountAsync(l => l.IsVerified, ct), false, "success"),
                    new("expiringSoon", await db.LawyerLicenses.CountAsync(l => l.VerificationStatus == LicenseVerificationStatus.Approved
                        && l.ExpiryDate >= today && l.ExpiryDate <= soon, ct), false, "danger"),
                ];
            }
            case "payments":
            {
                var collected = db.Payments.Where(p => p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PartiallyRefunded
                    || p.Status == PaymentStatus.Refunded);
                return
                [
                    new("collected", await collected.SumAsync(p => p.Total, ct), true),
                    new("thisMonth", await collected.Where(p => p.PaidAtUtc >= monthStart).SumAsync(p => p.Total, ct), true, "success"),
                    new("refunded", await db.Refunds.Where(r => r.Status == RefundStatus.Completed).SumAsync(r => r.Amount, ct), true, "danger"),
                    new("held", await db.Payouts.Where(o => o.Status == PayoutStatus.Held || o.Status == PayoutStatus.Suspended)
                        .SumAsync(o => o.Amount, ct), true, "gold"),
                ];
            }
            case "catalog":
                return
                [
                    new("categories", await db.ServiceCategories.CountAsync(c => c.IsActive, ct), false),
                    new("services", await db.ServiceCatalogItems.CountAsync(s => s.IsActive, ct), false, "success"),
                    new("hidden", await db.ServiceCatalogItems.CountAsync(s => !s.IsActive, ct), false),
                    new("requestsThisMonth", await db.CatalogRequests.CountAsync(r => r.CreatedAtUtc >= monthStart, ct), false, "gold"),
                ];
            case "discountCodes":
            {
                var confirmed = db.DiscountRedemptions.Where(r => r.Status == DiscountRedemptionStatus.Confirmed);
                return
                [
                    new("total", await db.DiscountCodes.CountAsync(ct), false),
                    new("activeNow", await db.DiscountCodes.CountAsync(c => c.IsActive && (c.StartsAtUtc == null || c.StartsAtUtc <= now)
                        && (c.EndsAtUtc == null || c.EndsAtUtc > now), ct), false, "success"),
                    new("redemptions", await confirmed.CountAsync(ct), false),
                    new("discountGiven", await confirmed.SumAsync(r => r.Amount, ct), true, "gold"),
                ];
            }
            case "users":
            {
                var admins = db.Users.Where(u => u.UserType == UserType.Admin);
                return
                [
                    new("total", await admins.CountAsync(ct), false),
                    new("active", await admins.CountAsync(u => u.Status == UserStatus.Active, ct), false, "success"),
                    new("suspended", await admins.CountAsync(u => u.Status == UserStatus.Suspended, ct), false, "danger"),
                    new("signedInThisWeek", await admins.CountAsync(u => u.LastLoginAtUtc >= weekAgo, ct), false, "gold"),
                ];
            }
            case "audit":
                return
                [
                    new("total", await db.AuditLogs.CountAsync(ct), false),
                    new("today", await db.AuditLogs.CountAsync(a => a.OccurredAtUtc >= dayStart, ct), false, "gold"),
                    new("thisWeek", await db.AuditLogs.CountAsync(a => a.OccurredAtUtc >= weekAgo, ct), false),
                    new("adminThisWeek", await db.AuditLogs.CountAsync(a => a.OccurredAtUtc >= weekAgo && a.ActorRole == "Admin", ct), false,
                        "success"),
                ];
            default:
                throw new KeyNotFoundException("Unknown page.");
        }
    }
}
