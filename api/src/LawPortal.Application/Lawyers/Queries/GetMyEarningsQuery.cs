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
                o.Id, o.Payment!.ServiceRequest!.Number, o.Amount, o.Status.ToString(), o.CreatedAtUtc, o.ReleasedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
