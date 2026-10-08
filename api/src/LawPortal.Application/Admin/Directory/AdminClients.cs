using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Domain.Identity;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Requests;
using LawPortal.Domain.Wallet;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Directory;

// «العملاء»: every client, and one client's full picture — requests, payments, wallet, activity.

public record AdminClientStatsDto(int Total, int ActiveThisMonth, decimal WalletTotal, decimal TotalPaid);

public record AdminClientSummaryDto(
    Guid ClientProfileId, Guid UserId, string? FullName, string? PhoneE164, string? CityNameAr, string? CityNameEn,
    int RequestsCount, decimal TotalPaid, decimal WalletBalance, DateTime? LastActivityUtc, string Status, DateTime JoinedAtUtc);

public record AdminClientListDto(AdminClientStatsDto Stats, PagedResult<AdminClientSummaryDto> Page);

/// <param name="Status">Active, Suspended or Deleted.</param>
public record GetAdminClientsQuery(string? Search = null, int? RegionId = null, int? CityId = null, string? Status = null, bool? HasWalletBalance = null,
    int Page = 1, int PageSize = 20) : IRequest<AdminClientListDto>;

/// <summary>What clients paid for real: settled payments minus completed refunds.</summary>
internal static class ClientMoney
{
    private static IQueryable<Payment> Settled(ILawPortalDbContext db) =>
        db.Payments.Where(p => p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PartiallyRefunded || p.Status == PaymentStatus.Refunded);

    public static async Task<decimal> TotalAsync(ILawPortalDbContext db, CancellationToken cancellationToken) =>
        (await Settled(db).SumAsync(p => (decimal?)p.Total, cancellationToken) ?? 0)
        - (await db.Refunds.Where(r => r.Status == RefundStatus.Completed).SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0);

    public static async Task<Dictionary<Guid, decimal>> PerClientAsync(ILawPortalDbContext db, IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken)
    {
        var paid = await Settled(db).Where(p => clientIds.Contains(p.ClientId))
            .GroupBy(p => p.ClientId).Select(g => new { g.Key, Sum = g.Sum(p => p.Total) }).ToListAsync(cancellationToken);
        var refunded = await db.Refunds.Where(r => r.Status == RefundStatus.Completed && clientIds.Contains(r.Payment!.ClientId))
            .GroupBy(r => r.Payment!.ClientId).Select(g => new { g.Key, Sum = g.Sum(r => r.Amount) }).ToDictionaryAsync(x => x.Key, x => x.Sum, cancellationToken);
        return paid.ToDictionary(x => x.Key, x => x.Sum - refunded.GetValueOrDefault(x.Key));
    }
}

public class GetAdminClientsHandler(ILawPortalDbContext db) : IRequestHandler<GetAdminClientsQuery, AdminClientListDto>
{
    public async Task<AdminClientListDto> Handle(GetAdminClientsQuery request, CancellationToken cancellationToken)
    {
        // Deleted clients are hidden by the soft-delete filter; admins see them too.
        var clients = db.ClientProfiles.IgnoreQueryFilters().Where(c => c.User!.UserType == UserType.Client);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            var byRequest = db.ServiceRequests.Where(r => r.Number.Contains(term)).Select(r => r.ClientId);
            clients = clients.Where(c => (c.FullName != null && c.FullName.Contains(term))
                || (c.User!.PhoneE164 != null && c.User.PhoneE164.Contains(Activity.PhoneDigits(term))) || byRequest.Contains(c.Id));
        }
        if (request.RegionId is { } regionId) clients = clients.Where(c => c.City != null && c.City.RegionId == regionId);
        if (request.CityId is { } cityId) clients = clients.Where(c => c.CityId == cityId);
        clients = request.Status switch
        {
            "Active" => clients.Where(c => !c.User!.IsDeleted && c.User.Status == UserStatus.Active),
            "Suspended" => clients.Where(c => !c.User!.IsDeleted && (c.User.Status == UserStatus.Suspended || c.User.Status == UserStatus.Banned)),
            "Deleted" => clients.Where(c => c.User!.IsDeleted),
            _ => clients,
        };
        if (request.HasWalletBalance == true)
            clients = clients.Where(c => db.Wallets.Any(w => w.UserId == c.UserId && w.Balance > 0));

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var allClients = db.ClientProfiles.IgnoreQueryFilters().Where(c => c.User!.UserType == UserType.Client);
        var stats = new AdminClientStatsDto(
            await allClients.CountAsync(cancellationToken),
            await allClients.CountAsync(c => c.User!.LastLoginAtUtc >= monthStart, cancellationToken),
            await db.Wallets.Where(w => allClients.Any(c => c.UserId == w.UserId)).SumAsync(w => (decimal?)w.Balance, cancellationToken) ?? 0,
            await ClientMoney.TotalAsync(db, cancellationToken));

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var total = await clients.CountAsync(cancellationToken);
        var rows = await clients
            .OrderByDescending(c => c.User!.LastLoginAtUtc ?? c.User.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new
            {
                c.Id, c.UserId, c.FullName, c.User!.PhoneE164, CityAr = c.City!.NameAr, CityEn = c.City.NameEn,
                c.User.Status, c.User.IsDeleted, c.User.LastLoginAtUtc, c.User.CreatedAtUtc,
                Requests = db.ServiceRequests.Count(r => r.ClientId == c.Id),
                Wallet = db.Wallets.Where(w => w.UserId == c.UserId).Select(w => (decimal?)w.Balance).FirstOrDefault() ?? 0,
            })
            .ToListAsync(cancellationToken);
        var ids = rows.Select(r => r.Id).ToList();
        var paid = await ClientMoney.PerClientAsync(db, ids, cancellationToken);

        var items = rows.Select(r => new AdminClientSummaryDto(r.Id, r.UserId, r.FullName, r.PhoneE164, r.CityAr, r.CityEn,
            r.Requests, paid.GetValueOrDefault(r.Id), r.Wallet, r.LastLoginAtUtc, AccountStatus.Of(r.Status, r.IsDeleted), r.CreatedAtUtc)).ToList();
        return new AdminClientListDto(stats, new PagedResult<AdminClientSummaryDto>(items, page, pageSize, total));
    }
}

public record AdminRequestRowDto(Guid Id, string Number, string Type, string? LawyerName, Guid? LawyerProfileId, string? ClientName,
    string? ClientPhoneE164, Guid ClientProfileId, decimal? Amount, string Status, DateTime CreatedAtUtc);

public record AdminClientPaymentDto(Guid Id, string Number, string Purpose, string Method, decimal Total, decimal Refunded, string Status, DateTime CreatedAtUtc);
public record AdminWalletMovementDto(string Type, decimal Amount, string? PaymentNumber, Guid? PaymentId, string? Description, DateTime CreatedAtUtc);
public record ActivityDto(string Action, string? Details, string? ActorRole, string? ActorName, DateTime OccurredAtUtc);

public record AdminClientDetailDto(
    AdminClientSummaryDto Summary, bool PledgeAccepted, DateTime? PledgeAcceptedAtUtc, decimal TotalRefunded,
    IReadOnlyList<AdminRequestRowDto> Requests, IReadOnlyList<AdminClientPaymentDto> Payments,
    IReadOnlyList<AdminWalletMovementDto> Wallet, IReadOnlyList<ActivityDto> Activity);

public record GetAdminClientQuery(Guid ClientProfileId) : IRequest<AdminClientDetailDto>;

public class GetAdminClientHandler(ILawPortalDbContext db, IMediator mediator) : IRequestHandler<GetAdminClientQuery, AdminClientDetailDto>
{
    public async Task<AdminClientDetailDto> Handle(GetAdminClientQuery request, CancellationToken cancellationToken)
    {
        var c = await db.ClientProfiles.IgnoreQueryFilters()
            .Include(x => x.User).Include(x => x.City)
            .FirstOrDefaultAsync(x => x.Id == request.ClientProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Client not found.");

        var payments = await db.Payments.Where(p => p.ClientId == c.Id).OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new AdminClientPaymentDto(p.Id, p.Number, p.Purpose.ToString(), p.MethodDescription, p.Total,
                p.Refunds.Where(r => r.Status == RefundStatus.Completed).Sum(r => (decimal?)r.Amount) ?? 0, p.Status.ToString(), p.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == c.UserId, cancellationToken);
        var movements = wallet is null ? [] : await db.WalletTransactions.Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new AdminWalletMovementDto(t.Type.ToString(),
                t.Type == WalletTransactionType.Payment ? -t.Amount : t.Amount,
                db.Payments.Where(p => p.Id == t.PaymentId).Select(p => p.Number).FirstOrDefault(), t.PaymentId, t.Description, t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var requests = (await mediator.Send(new GetAdminRequestsQuery(ClientProfileId: c.Id, PageSize: 50), cancellationToken)).Page.Items;
        var paidTotal = payments.Where(p => p.Status is "Paid" or "PartiallyRefunded" or "Refunded").Sum(p => p.Total - p.Refunded);
        var summary = new AdminClientSummaryDto(c.Id, c.UserId, c.FullName, c.User!.PhoneE164, c.City?.NameAr, c.City?.NameEn,
            requests.Count, paidTotal, wallet?.Balance ?? 0, c.User.LastLoginAtUtc, AccountStatus.Of(c.User.Status, c.User.IsDeleted), c.User.CreatedAtUtc);

        var paymentIds = payments.Select(p => p.Id.ToString()).ToList();
        var activity = await Activity.ForAsync(db, [c.Id.ToString(), c.UserId.ToString(), .. paymentIds], c.UserId, cancellationToken);

        return new AdminClientDetailDto(summary, c.HasAcceptedCurrentPledge, c.PledgeAcceptedAtUtc,
            payments.Sum(p => p.Refunded), requests, payments, movements, activity);
    }
}

/// <summary>Account status as one word for the admin screens.</summary>
public static class AccountStatus
{
    public static string Of(UserStatus status, bool deleted) =>
        deleted || status == UserStatus.Deleted ? "Deleted"
        : status is UserStatus.Suspended or UserStatus.Banned ? "Suspended"
        : status == UserStatus.PendingVerification ? "Pending"
        : "Active";
}

/// <summary>The audit log entries about any of these records or done by this user, newest first.</summary>
public static class Activity
{
    /// <summary>Lets an admin type a phone the local way: "0506…" matches "+966506…".</summary>
    public static string PhoneDigits(string term) => term.StartsWith('0') ? term[1..] : term;

    public static async Task<IReadOnlyList<ActivityDto>> ForAsync(ILawPortalDbContext db, IReadOnlyList<string> entityIds, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var rows = await db.AuditLogs
            .Where(a => (a.EntityId != null && entityIds.Contains(a.EntityId)) || (actorUserId != null && a.ActorUserId == actorUserId))
            .OrderByDescending(a => a.OccurredAtUtc).Take(100)
            .Select(a => new { a.Action, a.Details, a.ActorRole, a.ActorUserId, a.OccurredAtUtc })
            .ToListAsync(cancellationToken);
        var actorIds = rows.Where(r => r.ActorUserId != null).Select(r => r.ActorUserId!.Value).Distinct().ToList();
        var names = await db.Users.IgnoreQueryFilters().Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.AdminProfile != null ? u.AdminProfile.DisplayName : u.LawyerProfile != null ? u.LawyerProfile.FullName : u.ClientProfile != null ? u.ClientProfile.FullName : null, u.Email, u.PhoneE164 })
            .ToDictionaryAsync(u => u.Id, u => u.Name ?? u.Email ?? u.PhoneE164, cancellationToken);
        return rows.Select(r => new ActivityDto(r.Action, r.Details, r.ActorRole,
            r.ActorUserId is { } id ? names.GetValueOrDefault(id) : null, r.OccurredAtUtc)).ToList();
    }
}
