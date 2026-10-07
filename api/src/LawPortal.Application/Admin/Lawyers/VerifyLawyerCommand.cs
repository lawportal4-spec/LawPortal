using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Onboarding;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LawPortal.Application.Admin.Lawyers;

public record VerifyLawyerCommand(Guid LawyerProfileId) : IRequest<Unit>;

public class VerifyLawyerHandler(
    ILawPortalDbContext db, ICurrentUser currentUser, IAuditLogger auditLogger,
    ITokenService tokenService, IEmailSender emailSender, IConfiguration configuration)
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
        if (profile.License.VerificationStatus == LicenseVerificationStatus.Approved)
            throw new InvalidOperationException("This licence is already approved.");

        profile.License.VerificationStatus = LicenseVerificationStatus.Approved;
        profile.License.VerifiedByAdminUserId = currentUser.UserId;
        profile.License.VerifiedAtUtc = DateTime.UtcNow;

        // An already-active lawyer whose renewed licence was re-reviewed simply stays active. A new
        // one isn't let in yet: approval starts onboarding — verify the email, then pay the fee.
        if (profile.User!.Status == UserStatus.Active)
            profile.IsVerified = true;

        await auditLogger.LogAsync("LawyerLicenseApproved", nameof(LawyerProfile), profile.Id.ToString(), cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        if (profile.User.Status == UserStatus.PendingVerification)
        {
            if (profile.User.IsEmailVerified)
            {
                await LawyerOnboarding.AdvanceAfterEmailAsync(db, profile, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await LawyerOnboarding.SendVerificationEmailAsync(db, tokenService, emailSender, configuration, profile.User, profile.FullName, cancellationToken);
            }
        }

        return Unit.Value;
    }
}
