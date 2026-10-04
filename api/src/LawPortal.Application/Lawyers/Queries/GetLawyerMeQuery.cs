using LawPortal.Application.Admin.Lawyers;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Queries;

/// <summary>Who is signed in and where their registration stands. Until approved, the portal
/// shows a status screen instead of the dashboard: under review, returned for correction (with
/// the admin's checklist and the current licence values to edit), or rejected (with the reason).</summary>
public record LawyerMeDto(
    string FullName,
    bool IsApproved,
    string ReviewStatus,
    string? RejectionReason,
    IReadOnlyList<string> CorrectionIssues,
    string? CorrectionNote,
    string LicenseType,
    string LicenseNumber,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    string? DocumentFileName);

public record GetLawyerMeQuery : IRequest<LawyerMeDto>;

public class GetLawyerMeHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<GetLawyerMeQuery, LawyerMeDto>
{
    public async Task<LawyerMeDto> Handle(GetLawyerMeQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        var lawyer = await db.LawyerProfiles
            .Include(l => l.User)
            .Include(l => l.License)
            .FirstAsync(l => l.Id == lawyerProfileId, cancellationToken);
        var license = lawyer.License!;

        // Approval flips the user to Active (VerifyLawyerCommand); a later licence renewal only
        // resets the licence, so it must not lock an approved lawyer out of the portal.
        var isApproved = lawyer.User!.Status != UserStatus.PendingVerification;

        return new LawyerMeDto(
            lawyer.FullName,
            isApproved,
            isApproved ? LicenseVerificationStatus.Approved.ToString() : license.VerificationStatus.ToString(),
            license.RejectionReason,
            CorrectionIssueNames.Of(license.CorrectionIssues),
            license.CorrectionNote,
            license.LicenseType.ToString(),
            license.LicenseNumber,
            license.IssueDate,
            license.ExpiryDate,
            license.DocumentFileName);
    }
}
