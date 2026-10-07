using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Lawyers.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Queries;

public record GetMyEarningsQuery : IRequest<IReadOnlyList<PayoutSummaryDto>>;

public class GetMyEarningsHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyEarningsQuery, IReadOnlyList<PayoutSummaryDto>>
{
    public async Task<IReadOnlyList<PayoutSummaryDto>> Handle(GetMyEarningsQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        return await db.Payouts
            .Where(o => o.LawyerProfileId == lawyerProfileId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new PayoutSummaryDto(
                o.Id, o.Payment!.ServiceRequest!.Number, o.Amount, o.Status.ToString(), o.CreatedAtUtc, o.ReleasedAtUtc, o.DebtOffset))
            .ToListAsync(cancellationToken);
    }
}

public record GetMyDebtQuery : IRequest<MyDebtDto>;

public class GetMyDebtHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<GetMyDebtQuery, MyDebtDto>
{
    public async Task<MyDebtDto> Handle(GetMyDebtQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        var entries = await db.LawyerDebtEntries.Where(e => e.LawyerProfileId == lawyerProfileId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Select(e => new
            {
                e.Kind, e.Amount, e.CreatedAtUtc,
                RequestNumber = db.Payments.Where(p => p.Id == e.PaymentId).Select(p => p.ServiceRequest!.Number).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);
        return new MyDebtDto(entries.Sum(e => e.Amount),
            entries.Select(e => new MyDebtEntryDto(e.Kind.ToString(), e.Amount, e.RequestNumber, e.CreatedAtUtc)).ToList());
    }
}
