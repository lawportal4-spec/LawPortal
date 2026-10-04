using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Auth.Commands;

/// <summary>Confirms the phone a lawyer registered with. The account still waits for an admin
/// to approve the licence — this only unlocks sign-in.</summary>
public record VerifyLawyerRegistrationCommand(string PhoneE164, string Code) : IRequest<Unit>;

public class VerifyLawyerRegistrationValidator : AbstractValidator<VerifyLawyerRegistrationCommand>
{
    public VerifyLawyerRegistrationValidator()
    {
        RuleFor(x => x.PhoneE164).Matches(@"^\+9665\d{8}$");
        RuleFor(x => x.Code).Matches(@"^\d{6}$");
    }
}

public class VerifyLawyerRegistrationHandler(ILawPortalDbContext db, OtpService otpService, IAuditLogger auditLogger)
    : IRequestHandler<VerifyLawyerRegistrationCommand, Unit>
{
    public async Task<Unit> Handle(VerifyLawyerRegistrationCommand request, CancellationToken cancellationToken)
    {
        var user = await PendingLawyer.FindAsync(db, request.PhoneE164, cancellationToken);

        await otpService.VerifyAsync(request.PhoneE164, request.Code, OtpPurpose.LawyerRegistration, cancellationToken);
        user.IsPhoneVerified = true;
        await db.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync("LawyerPhoneVerified", nameof(User), user.Id.ToString(), cancellationToken: cancellationToken);
        return Unit.Value;
    }
}

/// <summary>"Resend" on the activation screen.</summary>
public record ResendLawyerRegistrationOtpCommand(string PhoneE164) : IRequest<Unit>;

public class ResendLawyerRegistrationOtpValidator : AbstractValidator<ResendLawyerRegistrationOtpCommand>
{
    public ResendLawyerRegistrationOtpValidator() => RuleFor(x => x.PhoneE164).Matches(@"^\+9665\d{8}$");
}

public class ResendLawyerRegistrationOtpHandler(ILawPortalDbContext db, OtpService otpService)
    : IRequestHandler<ResendLawyerRegistrationOtpCommand, Unit>
{
    public async Task<Unit> Handle(ResendLawyerRegistrationOtpCommand request, CancellationToken cancellationToken)
    {
        await PendingLawyer.FindAsync(db, request.PhoneE164, cancellationToken);
        await otpService.IssueAsync(request.PhoneE164, OtpPurpose.LawyerRegistration, cancellationToken);
        return Unit.Value;
    }
}

internal static class PendingLawyer
{
    public static async Task<User> FindAsync(ILawPortalDbContext db, string phoneE164, CancellationToken cancellationToken) =>
        await db.Users.FirstOrDefaultAsync(
            u => u.UserType == UserType.Lawyer && u.PhoneE164 == phoneE164 && !u.IsPhoneVerified, cancellationToken)
        ?? throw new InvalidOperationException("No pending registration for this number.");
}
