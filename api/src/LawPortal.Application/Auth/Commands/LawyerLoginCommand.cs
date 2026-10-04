using FluentValidation;
using LawPortal.Application.Auth.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Auth.Commands;

/// <summary>The web portal signs in by phone (as the registration wizard collects it); the
/// mobile app still sends email. Exactly one of the two identifies the account.</summary>
public record LawyerLoginCommand(string? PhoneE164, string? Email, string Password, string RecaptchaToken) : IRequest<AuthResultDto>;

public class LawyerLoginValidator : AbstractValidator<LawyerLoginCommand>
{
    public LawyerLoginValidator()
    {
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.PhoneE164) || !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Phone or email is required.");
        RuleFor(x => x.PhoneE164).Matches(@"^\+9665\d{8}$").When(x => !string.IsNullOrWhiteSpace(x.PhoneE164));
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.RecaptchaToken).NotEmpty();
    }
}

public class LawyerLoginHandler(
    ILawPortalDbContext db,
    IPasswordHasher passwordHasher,
    IRecaptchaVerifier recaptcha,
    TokenIssuer tokenIssuer,
    IAuditLogger auditLogger)
    : IRequestHandler<LawyerLoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(LawyerLoginCommand request, CancellationToken cancellationToken)
    {
        if (!await recaptcha.VerifyAsync(request.RecaptchaToken, cancellationToken))
            throw new ValidationException("reCAPTCHA verification failed.");

        var byPhone = !string.IsNullOrWhiteSpace(request.PhoneE164);
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.UserType == UserType.Lawyer && (byPhone ? u.PhoneE164 == request.PhoneE164 : u.Email == request.Email),
            cancellationToken);

        if (user is null || user.PasswordHash is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Incorrect phone number or password.");

        // Registered through the wizard but never entered the activation code.
        if (user.PhoneE164 is not null && !user.IsPhoneVerified)
            throw new UnauthorizedAccessException("This account has not been activated yet.");

        if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
            throw new UnauthorizedAccessException("This account is not active.");

        var result = await tokenIssuer.IssueAsync(user, cancellationToken);
        await auditLogger.LogAsync("LawyerLoggedIn", nameof(User), user.Id.ToString(), cancellationToken: cancellationToken);
        return result;
    }
}
