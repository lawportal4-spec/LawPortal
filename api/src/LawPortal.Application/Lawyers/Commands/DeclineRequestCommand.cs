using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Payments.Commands;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Commands;

/// <summary>Declining before accepting is a full, automatic refund — reuses the exact same
/// <c>RefundPaymentCommand</c> pipeline as an admin-issued refund (proportional-reversal ledger
/// posting, wallet credit if wallet-funded, status transition to <c>Refunded</c>), rather than a
/// parallel decline-specific money path.</summary>
public record DeclineRequestCommand(Guid RequestId, string Reason) : IRequest<Unit>;

public class DeclineRequestValidator : AbstractValidator<DeclineRequestCommand>
{
    public DeclineRequestValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
}

public class DeclineRequestHandler(ILawPortalDbContext db, ICurrentUser currentUser, ISender sender)
    : IRequestHandler<DeclineRequestCommand, Unit>
{
    public async Task<Unit> Handle(DeclineRequestCommand request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        var consultation = await db.ConsultationRequests
            .FirstOrDefaultAsync(c => c.Id == request.RequestId && c.LawyerProfileId == lawyerProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        if (consultation.Status != RequestStatus.Paid)
            throw new InvalidOperationException("Only a paid, unaccepted request can be declined.");

        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.ServiceRequestId == consultation.Id && p.Status == PaymentStatus.Paid, cancellationToken)
            ?? throw new InvalidOperationException("No paid payment found for this request.");

        await sender.Send(new RefundPaymentCommand(payment.Id, payment.Total, $"Lawyer declined: {request.Reason}"), cancellationToken);
        return Unit.Value;
    }
}
