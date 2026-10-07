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

public class VerifyClientOtpHandler(ILawPortalDbContext db, OtpService otpService, TokenIssuer tokenIssuer, IAuditLogger auditLogger)
    : IRequestHandler<VerifyClientOtpCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(VerifyClientOtpCommand request, CancellationToken cancellationToken)
    {
        await otpService.VerifyAsync(request.PhoneE164, request.Code, OtpPurpose.Login, cancellationToken);

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.PhoneE164 == request.PhoneE164, cancellationToken);

        // Lawyers register with a phone too; client OTP must never open a lawyer's account.
        if (user is { Status: UserStatus.Suspended or UserStatus.Banned })
            throw new InvalidOperationException("This account is suspended. Please contact support.");

        if (user is not null && user.UserType != UserType.Client)
            throw new InvalidOperationException("This number belongs to a lawyer account. Sign in through the lawyer portal.");

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
