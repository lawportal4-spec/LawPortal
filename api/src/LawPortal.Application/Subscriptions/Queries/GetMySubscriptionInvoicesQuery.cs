using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Subscriptions.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Subscriptions.Queries;

public record GetMySubscriptionInvoicesQuery : IRequest<IReadOnlyList<SubscriptionInvoiceDto>>;

public class GetMySubscriptionInvoicesHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMySubscriptionInvoicesQuery, IReadOnlyList<SubscriptionInvoiceDto>>
{
    public async Task<IReadOnlyList<SubscriptionInvoiceDto>> Handle(GetMySubscriptionInvoicesQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        return await db.SubscriptionInvoices
            .Where(i => i.LawyerSubscription!.LawyerProfileId == lawyerProfileId)
            .OrderByDescending(i => i.PeriodStartUtc)
            .Select(i => new SubscriptionInvoiceDto(
                i.Id, i.Number, i.PeriodStartUtc, i.PeriodEndUtc, i.SubtotalExVat, i.VatAmount, i.Total,
                i.Status.ToString(), i.DueAtUtc, i.PaidAtUtc))
            .ToListAsync(cancellationToken);
    }
}
