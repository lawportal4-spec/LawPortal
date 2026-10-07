using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers;
using LawPortal.Application.Payments;
using LawPortal.Domain.Identity;
using LawPortal.Domain.Payments;
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

        var user = await db.Users.Include(u => u.LawyerProfile).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException();

        // A lawyer may leave even with money held or owed (the deletion screen warned them):
        // shares not paid yet stop, debts stay on the books and keep being chased.
        if (user.LawyerProfile is { } lawyer)
        {
            var held = await db.Payouts.Where(o => o.LawyerProfileId == lawyer.Id && o.Status == PayoutStatus.Held).ToListAsync(cancellationToken);
            foreach (var payout in held) payout.Status = PayoutStatus.Suspended;

            var owes = await LawyerDebts.BalanceAsync(db, lawyer.Id, cancellationToken) > 0;
            db.DeletedAccountFingerprints.Add(new DeletedAccountFingerprint
            {
                Id = Guid.NewGuid(),
                FormerUserId = user.Id,
                LawyerProfileId = lawyer.Id,
                PhoneHash = IdentityFingerprints.Of(user.PhoneE164),
                EmailHash = IdentityFingerprints.Of(user.Email),
                NationalIdHash = IdentityFingerprints.Of(lawyer.NationalIdNumber),
                // Debt collection is a legitimate reason to keep contact details; only while they owe money.
                ContactEmail = owes ? user.Email : null,
                ContactPhone = owes ? user.PhoneE164 : null,
            });
            lawyer.NationalIdNumber = null;
            lawyer.IsVerified = false;
        }

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
