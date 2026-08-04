using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Requests.Commands;

public record SubmitRequestCommand(Guid RequestId) : IRequest<Unit>;

public class SubmitRequestHandler(ILawPortalDbContext db, ICurrentUser currentUser, IBidFanOutQueue bidFanOutQueue)
    : IRequestHandler<SubmitRequestCommand, Unit>
{
    public async Task<Unit> Handle(SubmitRequestCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        // EF Core resolves the correct TPT subtype (ConsultationRequest/CatalogRequest)
        // automatically — this handler only needs the shared base's fields.
        var draft = await db.ServiceRequests
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.ClientId == clientId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        if (string.IsNullOrWhiteSpace(draft.Title) || string.IsNullOrWhiteSpace(draft.Description))
            throw new InvalidOperationException("Title and details are required before submitting.");

        if (draft.Attachments.Any(a => a.ScanStatus == Domain.Files.AttachmentScanStatus.Pending))
            throw new InvalidOperationException("Wait for attachment scanning to finish before submitting.");

        if (draft.Attachments.Any(a => a.ScanStatus == Domain.Files.AttachmentScanStatus.Infected))
            throw new InvalidOperationException("One or more attachments failed the security scan and must be removed.");

        if (draft is ConsultationRequest consultation)
        {
            var pricing = await db.LawyerPricings.FirstOrDefaultAsync(p => p.LawyerProfileId == consultation.LawyerProfileId, cancellationToken)
                ?? throw new InvalidOperationException("The selected lawyer no longer has published pricing.");

            draft.Subtotal = consultation.ConsultationType switch
            {
                ConsultationType.Written => pricing.WrittenPrice,
                _ => consultation.SelectedDurationMinutes switch
                {
                    15 => pricing.Price15,
                    30 => pricing.Price30,
                    45 => pricing.Price45,
                    _ => throw new InvalidOperationException("A call consultation must have a selected duration."),
                },
            };
        }

        // BiddingRequest has no known price (and so no known lawyer to pay) at submission —
        // Subtotal stays null until an offer is accepted, same as a details-only catalog
        // request before a variant is chosen.
        draft.SubmittedAtUtc = DateTime.UtcNow;
        db.RequestStatusHistories.Add(draft.TransitionTo(RequestStatus.Submitted, "ClientSubmitted"));

        await db.SaveChangesAsync(cancellationToken);

        // Targeted invitations were already created at draft time (see CreateBiddingDraftHandler);
        // only a broadcast send needs the queue-based fan-out to potentially ~1,000 lawyers.
        if (draft is BiddingRequest { SendMethod: BidSendMethod.Broadcast } bidding)
            await bidFanOutQueue.EnqueueAsync(bidding.Id, cancellationToken);

        return Unit.Value;
    }
}
