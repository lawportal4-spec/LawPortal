using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Payments.Commands;

/// <summary>Manual release by an admin, for the exception: the lawyer did the work but never
/// marked the request complete (the normal trigger, <c>CompleteRequestCommand</c>). It can't be
/// undone and blocks refunds afterwards, so a written reason is required and goes to the audit log.</summary>
public record ReleasePayoutCommand(Guid PayoutId, string Reason) : IRequest<Unit>;

public class ReleasePayoutValidator : AbstractValidator<ReleasePayoutCommand>
{
    public ReleasePayoutValidator() => RuleFor(x => x.Reason).Must(r => r?.Trim().Length >= 10).WithMessage("Give a reason of at least 10 characters.").MaximumLength(500);
}

public class ReleasePayoutHandler(ILawPortalDbContext db, IAuditLogger auditLogger) : IRequestHandler<ReleasePayoutCommand, Unit>
{
    public async Task<Unit> Handle(ReleasePayoutCommand request, CancellationToken cancellationToken)
    {
        var payout = await db.Payouts.FirstOrDefaultAsync(o => o.Id == request.PayoutId, cancellationToken)
            ?? throw new KeyNotFoundException("Payout not found.");

        // Held, or Suspended because the lawyer deleted their account — an admin may still decide to pay it.
        if (payout.Status is not (PayoutStatus.Held or PayoutStatus.Suspended))
            throw new InvalidOperationException("Only a held or suspended payout can be released.");

        await LawyerDebts.ReleasePayoutAsync(db, payout, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("PayoutReleasedManually", nameof(Payout), payout.Id.ToString(),
            $"{payout.Amount:0.00} SAR · {request.Reason.Trim()}", cancellationToken);
        return Unit.Value;
    }
}
