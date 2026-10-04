using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Lawyers;

public record PendingLawyerDto(
    Guid LawyerProfileId,
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneE164,
    string LicenseNumber,
    string LicenseType,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    string VerificationStatus,
    string? LicenseDocumentUrl);

public record ListPendingLawyersQuery : IRequest<IReadOnlyList<PendingLawyerDto>>;

public class ListPendingLawyersHandler(ILawPortalDbContext db, IFileStorage storage) : IRequestHandler<ListPendingLawyersQuery, IReadOnlyList<PendingLawyerDto>>
{
    private static readonly TimeSpan DocumentUrlTtl = TimeSpan.FromMinutes(15);

    public async Task<IReadOnlyList<PendingLawyerDto>> Handle(ListPendingLawyersQuery request, CancellationToken cancellationToken)
    {
        var rows = await db.LawyerProfiles
            .Where(l => l.License!.VerificationStatus == Domain.Identity.LicenseVerificationStatus.PendingReview)
            // A registration whose phone was never activated isn't a real applicant yet.
            .Where(l => l.User!.PhoneE164 == null || l.User.IsPhoneVerified)
            .Select(l => new
            {
                l.Id,
                l.UserId,
                l.FullName,
                Email = l.User!.Email!,
                l.User.PhoneE164,
                l.License!.LicenseNumber,
                l.License.LicenseType,
                l.License.IssueDate,
                l.License.ExpiryDate,
                l.License.VerificationStatus,
                l.License.DocumentStorageKey,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new PendingLawyerDto(
                r.Id, r.UserId, r.FullName, r.Email, r.PhoneE164, r.LicenseNumber, r.LicenseType.ToString(),
                r.IssueDate, r.ExpiryDate, r.VerificationStatus.ToString(),
                r.DocumentStorageKey is null ? null : storage.CreateDownloadUrl(r.DocumentStorageKey, DocumentUrlTtl)))
            .ToList();
    }
}
