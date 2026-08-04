using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Commands;

public record AcceptRequestCommand(Guid RequestId) : IRequest<Unit>;

public class AcceptRequestHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<AcceptRequestCommand, Unit>
{
    public async Task<Unit> Handle(AcceptRequestCommand request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        var consultation = await db.ConsultationRequests
            .FirstOrDefaultAsync(c => c.Id == request.RequestId && c.LawyerProfileId == lawyerProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        if (consultation.Status != RequestStatus.Paid)
            throw new InvalidOperationException("Only a paid, unaccepted request can be accepted.");

        db.RequestStatusHistories.Add(consultation.TransitionTo(RequestStatus.InProgress, "LawyerAccepted"));
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
