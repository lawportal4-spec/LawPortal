using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Commands;

/// <summary>Re-enters the exact same admin verification queue built in P1 — no new admin
/// plumbing needed, since a renewal is just a licence back in <see cref="LicenseVerificationStatus.PendingReview"/>.</summary>
public record RenewLicenseCommand(string LicenseNumber, DateOnly IssueDate, DateOnly ExpiryDate) : IRequest<Unit>;

public class RenewLicenseValidator : AbstractValidator<RenewLicenseCommand>
{
    public RenewLicenseValidator()
    {
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ExpiryDate).GreaterThan(x => x.IssueDate);
    }
}

public class RenewLicenseHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<RenewLicenseCommand, Unit>
{
    public async Task<Unit> Handle(RenewLicenseCommand request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        var license = await db.LawyerLicenses.FirstOrDefaultAsync(l => l.LawyerProfileId == lawyerProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("No licence on file.");

        license.LicenseNumber = request.LicenseNumber;
        license.IssueDate = request.IssueDate;
        license.ExpiryDate = request.ExpiryDate;
        license.VerificationStatus = LicenseVerificationStatus.PendingReview;
        license.VerifiedByAdminUserId = null;
        license.VerifiedAtUtc = null;
        license.RejectionReason = null;

        var lawyer = await db.LawyerProfiles.FirstAsync(l => l.Id == lawyerProfileId, cancellationToken);
        lawyer.IsVerified = false;

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
