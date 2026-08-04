using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Lawyers;

public record PendingLawyerDto(
    Guid LawyerProfileId,
    Guid UserId,
    string FullName,
    string Email,
    string LicenseNumber,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    string VerificationStatus);

public record ListPendingLawyersQuery : IRequest<IReadOnlyList<PendingLawyerDto>>;

public class ListPendingLawyersHandler(ILawPortalDbContext db) : IRequestHandler<ListPendingLawyersQuery, IReadOnlyList<PendingLawyerDto>>
{
    public async Task<IReadOnlyList<PendingLawyerDto>> Handle(ListPendingLawyersQuery request, CancellationToken cancellationToken)
    {
        return await db.LawyerProfiles
            .Where(l => l.License!.VerificationStatus == Domain.Identity.LicenseVerificationStatus.PendingReview)
            .Select(l => new PendingLawyerDto(
                l.Id,
                l.UserId,
                l.FullName,
                l.User!.Email!,
                l.License!.LicenseNumber,
                l.License.IssueDate,
                l.License.ExpiryDate,
                l.License.VerificationStatus.ToString()))
            .ToListAsync(cancellationToken);
    }
}
