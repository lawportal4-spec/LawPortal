using FluentValidation;
using LawPortal.Application.Auth.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Auth.Commands;

public record LawyerLoginCommand(string Email, string Password, string RecaptchaToken) : IRequest<AuthResultDto>;

public class LawyerLoginValidator : AbstractValidator<LawyerLoginCommand>
{
    public LawyerLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
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

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Email == request.Email && u.UserType == UserType.Lawyer, cancellationToken);

        if (user is null || user.PasswordHash is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Incorrect email or password.");

        if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
            throw new UnauthorizedAccessException("This account is not active.");

        var result = await tokenIssuer.IssueAsync(user, cancellationToken);
        await auditLogger.LogAsync("LawyerLoggedIn", nameof(User), user.Id.ToString(), cancellationToken: cancellationToken);
        return result;
    }
}
