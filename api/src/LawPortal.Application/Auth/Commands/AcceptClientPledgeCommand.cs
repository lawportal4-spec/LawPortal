using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Auth.Commands;

/// <summary>Records that the signed-in client accepted the current platform pledge
/// (<see cref="ClientProfile.CurrentPledgeVersion"/>). Accepting again is a no-op.</summary>
public record AcceptClientPledgeCommand : IRequest<Unit>;

public class AcceptClientPledgeHandler(ILawPortalDbContext db, ICurrentUser currentUser, IAuditLogger auditLogger)
    : IRequestHandler<AcceptClientPledgeCommand, Unit>
{
    public async Task<Unit> Handle(AcceptClientPledgeCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var profile = await db.ClientProfiles.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Only client accounts accept the pledge.");
        if (profile.HasAcceptedCurrentPledge) return Unit.Value;

        profile.PledgeVersion = ClientProfile.CurrentPledgeVersion;
        profile.PledgeAcceptedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("ClientPledgeAccepted", nameof(ClientProfile), profile.Id.ToString(),
            cancellationToken: cancellationToken);
        return Unit.Value;
    }
}
