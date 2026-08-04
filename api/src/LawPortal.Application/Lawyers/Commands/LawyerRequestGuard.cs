using LawPortal.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Commands;

/// <summary>Mirrors <c>CreateConsultationDraftHandler.ResolveClientProfileIdAsync</c> for the
/// lawyer side of request-lifecycle commands.</summary>
internal static class LawyerRequestGuard
{
    public static async Task<Guid> ResolveLawyerProfileIdAsync(ILawPortalDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var lawyerProfileId = await db.LawyerProfiles.Where(l => l.UserId == userId).Select(l => l.Id).FirstOrDefaultAsync(cancellationToken);
        if (lawyerProfileId == Guid.Empty) throw new UnauthorizedAccessException("No lawyer profile for this account.");
        return lawyerProfileId;
    }
}
