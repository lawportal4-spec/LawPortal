using FluentValidation;
using LawPortal.Application.Auth.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.FreeMinutes;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Auth.Commands;

public record VerifyClientOtpCommand(string PhoneE164, string Code) : IRequest<AuthResultDto>;

public class VerifyClientOtpValidator : AbstractValidator<VerifyClientOtpCommand>
{
    public VerifyClientOtpValidator()
    {
        RuleFor(x => x.PhoneE164).Matches(@"^\+9665\d{8}$");
        RuleFor(x => x.Code).Matches(@"^\d{6}$");
    }
}

public class VerifyClientOtpHandler(ILawPortalDbContext db, TokenIssuer tokenIssuer, IAuditLogger auditLogger)
    : IRequestHandler<VerifyClientOtpCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(VerifyClientOtpCommand request, CancellationToken cancellationToken)
    {
        var challenge = await db.OtpChallenges
            .Where(o => o.PhoneE164 == request.PhoneE164 && o.ConsumedAtUtc == null)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge is null || challenge.IsExpired)
            throw new InvalidOperationException("No active code for this number. Request a new one.");

        if (challenge.IsLocked)
            throw new InvalidOperationException("Too many incorrect attempts. Request a new code.");

        var expectedHash = RequestClientOtpHandler.HashCode(request.PhoneE164, request.Code);
        if (challenge.CodeHash != expectedHash)
        {
            challenge.Attempts++;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Incorrect code.");
        }

        challenge.ConsumedAtUtc = DateTime.UtcNow;

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.PhoneE164 == request.PhoneE164, cancellationToken);

        var isNewUser = user is null;
        if (user is null)
        {
            user = User.CreateClient(request.PhoneE164);
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken); // need the generated Id before adding role/profile FK rows

            var clientRole = await db.Roles.SingleAsync(r => r.Name == RoleNames.Client, cancellationToken);
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = clientRole.Id });
            var clientProfileId = Guid.NewGuid();
            db.ClientProfiles.Add(new ClientProfile { Id = clientProfileId, UserId = user.Id });
            // "A free-minutes entitlement per user" (Baynah grants 7) — granted once, here, at
            // account creation. Nothing consumes it yet; metering against call duration is P8.
            db.FreeMinutesEntitlements.Add(new FreeMinutesEntitlement { Id = Guid.NewGuid(), ClientId = clientProfileId });
        }

        await db.SaveChangesAsync(cancellationToken);

        var result = await tokenIssuer.IssueAsync(user, cancellationToken);

        await auditLogger.LogAsync(
            isNewUser ? "ClientAccountCreated" : "ClientLoggedIn",
            nameof(User),
            user.Id.ToString(),
            cancellationToken: cancellationToken);

        return result;
    }
}
