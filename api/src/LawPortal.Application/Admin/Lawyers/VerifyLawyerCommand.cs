using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Lawyers;

public record VerifyLawyerCommand(Guid LawyerProfileId) : IRequest<Unit>;

public class VerifyLawyerHandler(ILawPortalDbContext db, ICurrentUser currentUser, IAuditLogger auditLogger)
    : IRequestHandler<VerifyLawyerCommand, Unit>
{
    public async Task<Unit> Handle(VerifyLawyerCommand request, CancellationToken cancellationToken)
    {
        var profile = await db.LawyerProfiles
            .Include(l => l.License)
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == request.LawyerProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Lawyer profile not found.");

        if (profile.License is null) throw new InvalidOperationException("No licence submitted for this lawyer.");

        profile.IsVerified = true;
        profile.License.VerificationStatus = LicenseVerificationStatus.Approved;
        profile.License.VerifiedByAdminUserId = currentUser.UserId;
        profile.License.VerifiedAtUtc = DateTime.UtcNow;
        profile.User!.Status = UserStatus.Active;

        await auditLogger.LogAsync("LawyerLicenseApproved", nameof(LawyerProfile), profile.Id.ToString(), cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
