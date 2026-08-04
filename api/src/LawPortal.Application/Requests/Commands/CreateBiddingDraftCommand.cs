using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Catalog;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Requests.Commands;

/// <summary>Steps 1–2 (service/specialty) and 4–5 (send method/lawyer targeting) of the
/// documented 6-step bidding wizard collapse into one draft call, same shape as
/// <see cref="CreateConsultationDraftCommand"/>. Targeted invitations are created immediately —
/// harmless while the request is still a Draft, exactly like a consultation's LawyerProfileId
/// being set before submission — so no separate storage is needed for "who was picked" between
/// draft and submit.</summary>
public record CreateBiddingDraftCommand(
    int ServiceId,
    int SpecialtyId,
    int? SubSpecialtyId,
    BidSendMethod SendMethod,
    IReadOnlyList<Guid>? TargetedLawyerProfileIds) : IRequest<Guid>;

public class CreateBiddingDraftValidator : AbstractValidator<CreateBiddingDraftCommand>
{
    public CreateBiddingDraftValidator()
    {
        RuleFor(x => x.ServiceId).GreaterThan(0);
        RuleFor(x => x.SpecialtyId).GreaterThan(0);

        RuleFor(x => x.TargetedLawyerProfileIds)
            .Must(ids => ids is { Count: > 0 })
            .When(x => x.SendMethod == BidSendMethod.Targeted)
            .WithMessage("Select at least one lawyer to send a targeted bidding request to.");

        RuleFor(x => x.TargetedLawyerProfileIds)
            .Must(ids => ids is null or { Count: 0 })
            .When(x => x.SendMethod == BidSendMethod.Broadcast)
            .WithMessage("A broadcast bidding request does not target specific lawyers.");
    }
}

public class CreateBiddingDraftHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CreateBiddingDraftCommand, Guid>
{
    public async Task<Guid> Handle(CreateBiddingDraftCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var service = await db.ServiceCatalogItems
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Service not found.");

        if (service.PricingModel != ServicePricingModel.CompetitiveBidding)
            throw new InvalidOperationException("This service does not use the bidding flow.");

        var draft = new BiddingRequest
        {
            Id = Guid.NewGuid(),
            Number = await RequestNumberGenerator.NextAsync(db, cancellationToken),
            ClientId = clientId,
            ServiceId = service.Id,
            SpecialtyId = request.SpecialtyId,
            SubSpecialtyId = request.SubSpecialtyId,
            SendMethod = request.SendMethod,
        };
        db.BiddingRequests.Add(draft);

        if (request.SendMethod == BidSendMethod.Targeted)
        {
            var validLawyerIds = await db.LawyerProfiles
                .Where(l => request.TargetedLawyerProfileIds!.Contains(l.Id) && l.IsVerified)
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);

            if (validLawyerIds.Count == 0)
                throw new InvalidOperationException("None of the selected lawyers could be found.");

            foreach (var lawyerId in validLawyerIds)
            {
                db.RequestInvitations.Add(new RequestInvitation
                {
                    Id = Guid.NewGuid(),
                    ServiceRequestId = draft.Id,
                    LawyerProfileId = lawyerId,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return draft.Id;
    }
}
