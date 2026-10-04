using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Auth.Commands;

/// <summary>"Forgot password", step 1: text a reset code to the lawyer's phone. Always succeeds
/// from the caller's point of view, so the endpoint can't be used to find out which numbers
/// belong to a lawyer; the OTP rate limits still apply to real accounts.</summary>
public record RequestLawyerPasswordResetCommand(string PhoneE164) : IRequest<Unit>;

public class RequestLawyerPasswordResetValidator : AbstractValidator<RequestLawyerPasswordResetCommand>
{
    public RequestLawyerPasswordResetValidator() => RuleFor(x => x.PhoneE164).Matches(@"^\+9665\d{8}$");
}

public class RequestLawyerPasswordResetHandler(ILawPortalDbContext db, OtpService otpService)
    : IRequestHandler<RequestLawyerPasswordResetCommand, Unit>
{
    public async Task<Unit> Handle(RequestLawyerPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var exists = await db.Users.AnyAsync(u => u.UserType == UserType.Lawyer && u.PhoneE164 == request.PhoneE164, cancellationToken);
        if (!exists) return Unit.Value;

        try
        {
            await otpService.IssueAsync(request.PhoneE164, OtpPurpose.PasswordReset, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Cooldown / daily cap: reporting it would tell the caller this number is registered.
            // The page runs its own 60s resend countdown, so a quiet no-op loses nothing.
        }
        return Unit.Value;
    }
}

/// <summary>"Forgot password", step 2: the code proves the phone, then the new password replaces
/// the old one and every existing session is signed out.</summary>
public record ResetLawyerPasswordCommand(string PhoneE164, string Code, string NewPassword) : IRequest<Unit>;

public class ResetLawyerPasswordValidator : AbstractValidator<ResetLawyerPasswordCommand>
{
    public ResetLawyerPasswordValidator()
    {
        RuleFor(x => x.PhoneE164).Matches(@"^\+9665\d{8}$");
        RuleFor(x => x.Code).Matches(@"^\d{6}$");
        RuleFor(x => x.NewPassword).LawyerPassword();
    }
}

public class ResetLawyerPasswordHandler(ILawPortalDbContext db, OtpService otpService, IPasswordHasher passwordHasher, IAuditLogger auditLogger)
    : IRequestHandler<ResetLawyerPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ResetLawyerPasswordCommand request, CancellationToken cancellationToken)
    {
        // Same message as a bad code, so this step doesn't reveal registered numbers either.
        var user = await db.Users.FirstOrDefaultAsync(
                u => u.UserType == UserType.Lawyer && u.PhoneE164 == request.PhoneE164, cancellationToken)
            ?? throw new InvalidOperationException("No active code for this number. Request a new one.");

        await otpService.VerifyAsync(request.PhoneE164, request.Code, OtpPurpose.PasswordReset, cancellationToken);

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        // The code proved the phone — lets a sign-up that never entered its activation code recover too.
        user.IsPhoneVerified = true;

        var activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens) token.RevokedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("LawyerPasswordReset", nameof(User), user.Id.ToString(), cancellationToken: cancellationToken);
        return Unit.Value;
    }
}
