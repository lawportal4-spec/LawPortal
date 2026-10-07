using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Payments;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Commands;

/// <summary>The real trigger for payout release — replaces P4's admin-triggered stopgap
/// (<c>ReleasePayoutCommand</c> still exists for disputes/manual intervention, but this is the
/// normal path now that a lawyer-side "mark complete" action exists). Covers both
/// <see cref="ConsultationRequest"/> (Paid → InProgress → Completed, via
/// <see cref="AcceptRequestCommand"/> first) and <see cref="BiddingRequest"/> (Paid → Completed
/// directly — no separate accept step exists for bidding, since the lawyer already committed by
/// having their offer accepted).</summary>
public record CompleteRequestCommand(Guid RequestId) : IRequest<Unit>;

public class CompleteRequestHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<CompleteRequestCommand, Unit>
{
    public async Task<Unit> Handle(CompleteRequestCommand request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        var serviceRequest = await db.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        var (isOwner, expectedFromStatus) = serviceRequest switch
        {
            ConsultationRequest c => (c.LawyerProfileId == lawyerProfileId, RequestStatus.InProgress),
            BiddingRequest b => (b.AwardedLawyerProfileId == lawyerProfileId, RequestStatus.Paid),
            _ => (false, RequestStatus.Draft),
        };
        if (!isOwner) throw new KeyNotFoundException("Request not found.");

        if (serviceRequest.Status != expectedFromStatus)
            throw new InvalidOperationException($"Only a {expectedFromStatus} request can be marked complete.");

        db.RequestStatusHistories.Add(serviceRequest.TransitionTo(RequestStatus.Completed, "LawyerCompleted"));

        var payout = await db.Payouts
            .FirstOrDefaultAsync(o => o.Payment!.ServiceRequestId == serviceRequest.Id && o.Status == PayoutStatus.Held, cancellationToken);
        if (payout is not null)
        {
            await LawyerDebts.ReleasePayoutAsync(db, payout, cancellationToken);
        }

        var lawyer = await db.LawyerProfiles.FirstAsync(l => l.Id == lawyerProfileId, cancellationToken);
        lawyer.CompletedRequestCount++;

        if (serviceRequest.SpecialtyId is { } specialtyId)
        {
            var lawyerSpecialty = await db.LawyerSpecialties
                .FirstOrDefaultAsync(ls => ls.LawyerProfileId == lawyerProfileId && ls.SpecialtyId == specialtyId, cancellationToken);
            if (lawyerSpecialty is not null) lawyerSpecialty.RequestCount++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
