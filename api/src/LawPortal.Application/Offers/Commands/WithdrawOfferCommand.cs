using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Offers.Commands;

public record WithdrawOfferCommand(Guid OfferId) : IRequest<Unit>;

public class WithdrawOfferHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<WithdrawOfferCommand, Unit>
{
    public async Task<Unit> Handle(WithdrawOfferCommand request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        var offer = await db.Offers
            .FirstOrDefaultAsync(o => o.Id == request.OfferId && o.LawyerProfileId == lawyerProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Offer not found.");

        if (offer.Status != OfferStatus.Pending)
            throw new InvalidOperationException("Only a pending offer can be withdrawn.");

        offer.Status = OfferStatus.Withdrawn;
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
