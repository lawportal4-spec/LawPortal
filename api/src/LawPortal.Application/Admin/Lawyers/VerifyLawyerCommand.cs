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

        // The same person came back after leaving with a debt: it follows them to this account.
        if (profile.PossibleFormerProfileId is { } formerId
            && await Payments.LawyerDebts.BalanceAsync(db, formerId, cancellationToken) is > 0 and var carried)
        {
            db.LawyerDebtEntries.Add(new Domain.Payments.LawyerDebtEntry
            {
                Id = Guid.NewGuid(), LawyerProfileId = formerId, Kind = Domain.Payments.LawyerDebtEntryKind.TransferredOut,
                Amount = -carried, Reference = profile.Id.ToString(), CreatedByUserId = currentUser.UserId,
            });
            db.LawyerDebtEntries.Add(new Domain.Payments.LawyerDebtEntry
            {
                Id = Guid.NewGuid(), LawyerProfileId = profile.Id, Kind = Domain.Payments.LawyerDebtEntryKind.TransferredIn,
                Amount = carried, Reference = formerId.ToString(), CreatedByUserId = currentUser.UserId,
            });
            await auditLogger.LogAsync("LawyerDebtCarriedOver", nameof(LawyerProfile), profile.Id.ToString(),
                $"{carried:0.00} SAR from {formerId}", cancellationToken);
        }

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
                try
                {
                    await LawyerOnboarding.SendVerificationEmailAsync(db, tokenService, emailSender, configuration, profile.User, profile.FullName, cancellationToken);
                }
                catch (InvalidOperationException)
                {
                    // The approval stands; the sender has logged why. The lawyer's "check your email"
                    // screen has a resend button that issues a fresh link.
                }
            }
        }

        return Unit.Value;
    }
}
