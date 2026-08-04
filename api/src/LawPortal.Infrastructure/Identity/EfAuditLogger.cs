using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Audit;
using LawPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace LawPortal.Infrastructure.Identity;

public class EfAuditLogger(LawPortalDbContext db, ICurrentUser currentUser, IHttpContextAccessor httpContextAccessor)
    : IAuditLogger
{
    public async Task LogAsync(
        string action,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = currentUser.UserId,
            ActorRole = currentUser.UserType?.ToString(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            Ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
