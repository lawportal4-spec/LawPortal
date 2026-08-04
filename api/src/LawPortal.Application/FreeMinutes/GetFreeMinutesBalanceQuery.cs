using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Requests.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.FreeMinutes;

public record FreeMinutesBalanceDto(int GrantedSeconds, int ConsumedSeconds, int RemainingSeconds);

public record GetFreeMinutesBalanceQuery : IRequest<FreeMinutesBalanceDto>;

public class GetFreeMinutesBalanceHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetFreeMinutesBalanceQuery, FreeMinutesBalanceDto>
{
    public async Task<FreeMinutesBalanceDto> Handle(GetFreeMinutesBalanceQuery request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var entitlement = await db.FreeMinutesEntitlements.FirstOrDefaultAsync(e => e.ClientId == clientId, cancellationToken);
        if (entitlement is null)
            return new FreeMinutesBalanceDto(0, 0, 0);

        return new FreeMinutesBalanceDto(
            entitlement.GrantedSeconds, entitlement.ConsumedSeconds,
            Math.Max(0, entitlement.GrantedSeconds - entitlement.ConsumedSeconds));
    }
}
