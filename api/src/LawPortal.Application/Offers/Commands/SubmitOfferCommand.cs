using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Offers.Commands;

/// <summary>The lawyer's ask on an invited bidding request — offers are "negotiable, not sealed
/// bids" (the plan's own reading of the docs), so calling this again on an existing Pending offer
/// just appends another revision (the lawyer revising their own price before the client has
/// responded) rather than erroring.</summary>
public record SubmitOfferCommand(Guid RequestId, decimal Amount, string? Message) : IRequest<Guid>;

public class SubmitOfferValidator : AbstractValidator<SubmitOfferCommand>
{
    public SubmitOfferValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Message).MaximumLength(2000);
    }
}

public class SubmitOfferHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<SubmitOfferCommand, Guid>
{
    private static readonly TimeSpan OfferLifetime = TimeSpan.FromDays(7);

    public async Task<Guid> Handle(SubmitOfferCommand request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        var biddingRequest = await db.BiddingRequests
            .FirstOrDefaultAsync(b => b.Id == request.RequestId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        if (biddingRequest.Status != RequestStatus.Submitted)
            throw new InvalidOperationException("This request is no longer accepting offers.");

        var isInvited = await db.RequestInvitations
            .AnyAsync(i => i.ServiceRequestId == request.RequestId && i.LawyerProfileId == lawyerProfileId, cancellationToken);
        if (!isInvited)
            throw new UnauthorizedAccessException("You were not invited to bid on this request.");

        var offer = await db.Offers
            .FirstOrDefaultAsync(o => o.ServiceRequestId == request.RequestId && o.LawyerProfileId == lawyerProfileId, cancellationToken);

        if (offer is not null && offer.Status != OfferStatus.Pending)
            throw new InvalidOperationException($"Your offer on this request is already {offer.Status} and cannot be revised.");

        if (offer is null)
        {
            offer = new Domain.Requests.Offer
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = request.RequestId,
                LawyerProfileId = lawyerProfileId,
                ExpiresAtUtc = DateTime.UtcNow.Add(OfferLifetime),
            };
            db.Offers.Add(offer);
        }

        db.OfferRevisions.Add(new OfferRevision
        {
            Id = Guid.NewGuid(),
            OfferId = offer.Id,
            Amount = request.Amount,
            ProposedBy = OfferProposedBy.Lawyer,
            Message = request.Message,
        });

        var invitation = await db.RequestInvitations
            .FirstAsync(i => i.ServiceRequestId == request.RequestId && i.LawyerProfileId == lawyerProfileId, cancellationToken);
        invitation.ViewedAtUtc ??= DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return offer.Id;
    }
}
