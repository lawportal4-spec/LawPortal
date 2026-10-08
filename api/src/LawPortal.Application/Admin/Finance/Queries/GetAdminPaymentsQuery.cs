using LawPortal.Application.Admin.Finance.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Finance.Queries;

/// <summary>Bound from the query string. <c>Search</c> matches the payment or request number, the
/// client's name or phone, or the lawyer's name. <c>Method</c> is "Card" or "Wallet".
/// <c>Payout</c>: Held or Released (the lawyer's share), or None (no payout, e.g. a wallet top-up).</summary>
public record GetAdminPaymentsQuery(
    PaymentStatus? Status = null,
    string? Search = null,
    PaymentPurpose? Purpose = null,
    string? Method = null,
    DateTime? From = null,
    DateTime? To = null,
    decimal? MinTotal = null,
    decimal? MaxTotal = null,
    bool? HasDiscount = null,
    string? Payout = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<AdminPaymentSummaryDto>>;

public class GetAdminPaymentsHandler(ILawPortalDbContext db) : IRequestHandler<GetAdminPaymentsQuery, PagedResult<AdminPaymentSummaryDto>>
{
    public async Task<PagedResult<AdminPaymentSummaryDto>> Handle(GetAdminPaymentsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var query = db.Payments.AsQueryable();
        if (request.Status is { } status) query = query.Where(p => p.Status == status);
        if (request.Purpose is { } purpose) query = query.Where(p => p.Purpose == purpose);
        if (!string.IsNullOrWhiteSpace(request.Method)) query = query.Where(p => p.MethodDescription == request.Method);
        if (request.From is { } from) query = query.Where(p => p.CreatedAtUtc >= from);
        if (request.To is { } to) query = query.Where(p => p.CreatedAtUtc <= to);
        if (request.MinTotal is { } min) query = query.Where(p => p.Total >= min);
        if (request.MaxTotal is { } max) query = query.Where(p => p.Total <= max);
        query = request.Payout switch
        {
            "Held" => query.Where(p => p.Payout != null && p.Payout.Status == PayoutStatus.Held),
            "Released" => query.Where(p => p.Payout != null && p.Payout.Status == PayoutStatus.Released),
            "None" => query.Where(p => p.Payout == null),
            _ => query,
        };
        if (request.HasDiscount is { } hasDiscount) query = query.Where(p => (p.DiscountAmount > 0) == hasDiscount);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p =>
                p.Number.Contains(term) ||
                (p.ServiceRequest != null && p.ServiceRequest.Number.Contains(term)) ||
                (p.Client != null && ((p.Client.FullName != null && p.Client.FullName.Contains(term)) ||
                                      (p.Client.User != null && p.Client.User.PhoneE164 != null && p.Client.User.PhoneE164.Contains(term)))) ||
                (p.LawyerProfile != null && p.LawyerProfile.FullName.Contains(term)));
        }
        query = query.OrderByDescending(p => p.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var page_ = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Client).ThenInclude(c => c!.User)
            .Include(p => p.LawyerProfile)
            .Include(p => p.ServiceRequest)
            .ToListAsync(cancellationToken);

        // Clients are phone-first and passwordless — most never set a display name, so the
        // phone number is the only identifier an admin actually has for them most of the time.
        var items = page_.Select(p => new AdminPaymentSummaryDto(
            p.Id, p.Number, p.Purpose.ToString(), p.Status.ToString(), p.Total,
            p.Client?.FullName ?? p.Client?.User?.PhoneE164, p.LawyerProfile?.FullName, p.ServiceRequest?.Number,
            p.CreatedAtUtc, p.PaidAtUtc)).ToList();

        return new PagedResult<AdminPaymentSummaryDto>(items, page, pageSize, totalCount);
    }
}
