using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Lawyers;

public record RejectLawyerCommand(Guid LawyerProfileId, string Reason) : IRequest<Unit>;

public class RejectLawyerValidator : AbstractValidator<RejectLawyerCommand>
{
    public RejectLawyerValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
}

public class RejectLawyerHandler(ILawPortalDbContext db, ICurrentUser currentUser, IAuditLogger auditLogger)
    : IRequestHandler<RejectLawyerCommand, Unit>
{
    public async Task<Unit> Handle(RejectLawyerCommand request, CancellationToken cancellationToken)
    {
        var profile = await db.LawyerProfiles
            .Include(l => l.License)
            .FirstOrDefaultAsync(l => l.Id == request.LawyerProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Lawyer profile not found.");

        if (profile.License is null) throw new InvalidOperationException("No licence submitted for this lawyer.");

        profile.License.VerificationStatus = LicenseVerificationStatus.Rejected;
        profile.License.RejectionReason = request.Reason;
        profile.License.VerifiedByAdminUserId = currentUser.UserId;
        profile.License.VerifiedAtUtc = DateTime.UtcNow;

        await auditLogger.LogAsync(
            "LawyerLicenseRejected", nameof(LawyerProfile), profile.Id.ToString(), request.Reason, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
