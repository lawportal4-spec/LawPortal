using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Requests.Commands;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Attachments;

/// <summary>Lets the client drop an attachment before submitting — the only way to recover
/// from a rejected (Infected) upload, since <see cref="RequestStatus.Draft"/> requests can't
/// be submitted while one is attached.</summary>
public record RemoveAttachmentCommand(Guid RequestId, Guid AttachmentId) : IRequest<Unit>;

public class RemoveAttachmentHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<RemoveAttachmentCommand, Unit>
{
    public async Task<Unit> Handle(RemoveAttachmentCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var draft = await db.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.ClientId == clientId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        if (draft.Status != RequestStatus.Draft)
            throw new InvalidOperationException("Cannot remove attachments from a submitted request.");

        var attachment = await db.RequestAttachments
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId && a.ServiceRequestId == draft.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Attachment not found.");

        db.RequestAttachments.Remove(attachment);
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
