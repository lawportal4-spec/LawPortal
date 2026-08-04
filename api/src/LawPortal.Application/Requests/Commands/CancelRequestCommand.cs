using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Requests.Commands;

public record CancelRequestCommand(Guid RequestId, string? Reason) : IRequest<Unit>;

public class CancelRequestValidator : AbstractValidator<CancelRequestCommand>
{
    public CancelRequestValidator() => RuleFor(x => x.Reason).MaximumLength(500);
}

public class CancelRequestHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CancelRequestCommand, Unit>
{
    public async Task<Unit> Handle(CancelRequestCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var serviceRequest = await db.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.ClientId == clientId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        serviceRequest.CancelReason = request.Reason;
        serviceRequest.CancelledAtUtc = DateTime.UtcNow;
        db.RequestStatusHistories.Add(serviceRequest.TransitionTo(RequestStatus.Cancelled, "ClientCancelled"));

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
