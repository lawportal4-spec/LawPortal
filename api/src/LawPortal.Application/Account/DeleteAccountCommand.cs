using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Account;

/// <summary>Soft-deletes the caller's own account and immediately anonymizes PII. A real async
/// purge job (financial-record retention rules, etc.) belongs in a later phase once those
/// retention rules are defined — this pass makes the deletion itself immediate and verifiable,
/// which is what the store-review requirement actually tests for.</summary>
public record DeleteAccountCommand : IRequest<Unit>;

public class DeleteAccountHandler(ILawPortalDbContext db, ICurrentUser currentUser, IAuditLogger auditLogger)
    : IRequestHandler<DeleteAccountCommand, Unit>
{
    public async Task<Unit> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException();

        var activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens) token.RevokedAtUtc = DateTime.UtcNow;

        user.IsDeleted = true;
        user.DeletedAtUtc = DateTime.UtcNow;
        user.Status = Domain.Identity.UserStatus.Deleted;
        // Anonymize immediately rather than leaving PII behind a soft-delete flag. Nulling
        // (rather than a placeholder string) also sidesteps the unique-index/max-length
        // constraints on these columns — MySQL permits multiple NULLs under a unique index.
        user.PhoneE164 = null;
        user.Email = null;
        user.PasswordHash = null;

        await auditLogger.LogAsync("AccountDeleted", "User", userId.ToString(), cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
