using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Queries;

/// <summary>Who is signed in and whether an admin has approved them yet — the portal shows a
/// "registration under review" screen instead of the dashboard until then.</summary>
public record LawyerMeDto(string FullName, bool IsApproved);

public record GetLawyerMeQuery : IRequest<LawyerMeDto>;

public class GetLawyerMeHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<GetLawyerMeQuery, LawyerMeDto>
{
    public async Task<LawyerMeDto> Handle(GetLawyerMeQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        return await db.LawyerProfiles
            .Where(l => l.Id == lawyerProfileId)
            // Approval flips the user to Active (VerifyLawyerCommand); a later licence renewal only
            // resets IsVerified, so it must not lock an approved lawyer out of the portal.
            .Select(l => new LawyerMeDto(l.FullName, l.User!.Status != UserStatus.PendingVerification))
            .FirstAsync(cancellationToken);
    }
}
