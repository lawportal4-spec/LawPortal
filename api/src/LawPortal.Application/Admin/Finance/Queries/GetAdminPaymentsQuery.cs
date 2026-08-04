using LawPortal.Application.Admin.Finance.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Finance.Queries;

public record GetAdminPaymentsQuery(PaymentStatus? Status, int Page, int PageSize) : IRequest<PagedResult<AdminPaymentSummaryDto>>;

public class GetAdminPaymentsHandler(ILawPortalDbContext db) : IRequestHandler<GetAdminPaymentsQuery, PagedResult<AdminPaymentSummaryDto>>
{
    public async Task<PagedResult<AdminPaymentSummaryDto>> Handle(GetAdminPaymentsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var query = db.Payments.AsQueryable();
        if (request.Status is { } status) query = query.Where(p => p.Status == status);
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
