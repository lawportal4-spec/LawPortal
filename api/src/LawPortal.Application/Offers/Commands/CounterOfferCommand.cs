using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Requests.Commands;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Offers.Commands;

/// <summary>The client's side of the negotiation — the mirror of
/// <see cref="SubmitOfferCommand"/>, appending a revision as <see cref="OfferProposedBy.Client"/>
/// instead of creating a new offer.</summary>
public record CounterOfferCommand(Guid OfferId, decimal Amount, string? Message) : IRequest<Unit>;

public class CounterOfferValidator : AbstractValidator<CounterOfferCommand>
{
    public CounterOfferValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Message).MaximumLength(2000);
    }
}

public class CounterOfferHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<CounterOfferCommand, Unit>
{
    public async Task<Unit> Handle(CounterOfferCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var offer = await db.Offers
            .FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken)
            ?? throw new KeyNotFoundException("Offer not found.");

        var owned = await db.BiddingRequests.AnyAsync(b => b.Id == offer.ServiceRequestId && b.ClientId == clientId, cancellationToken);
        if (!owned) throw new KeyNotFoundException("Offer not found.");

        if (offer.Status != OfferStatus.Pending)
            throw new InvalidOperationException($"This offer is {offer.Status} and can no longer be countered.");

        if (offer.ExpiresAtUtc < DateTime.UtcNow)
            throw new InvalidOperationException("This offer has expired.");

        db.OfferRevisions.Add(new OfferRevision
        {
            Id = Guid.NewGuid(),
            OfferId = offer.Id,
            Amount = request.Amount,
            ProposedBy = OfferProposedBy.Client,
            Message = request.Message,
        });

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
