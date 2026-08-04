using FluentValidation;
using LawPortal.Application.Auth.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Auth.Commands;

/// <summary>The documented 3-step lawyer registration collapsed into one command — step 1
/// (personal data), step 2 (region/city), and step 3 (licence) all submit together here;
/// the wizard UI can still present them as three screens against one draft on the client side.</summary>
public record RegisterLawyerCommand(
    string FullName,
    string Email,
    string Password,
    int? RegionId,
    int? CityId,
    string LicenseNumber,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    string RecaptchaToken) : IRequest<AuthResultDto>;

public class RegisterLawyerValidator : AbstractValidator<RegisterLawyerCommand>
{
    public RegisterLawyerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain a digit.");
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ExpiryDate).GreaterThan(x => x.IssueDate);
        RuleFor(x => x.RecaptchaToken).NotEmpty();
    }
}

public class RegisterLawyerHandler(
    ILawPortalDbContext db,
    IPasswordHasher passwordHasher,
    IRecaptchaVerifier recaptcha,
    TokenIssuer tokenIssuer,
    IAuditLogger auditLogger)
    : IRequestHandler<RegisterLawyerCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterLawyerCommand request, CancellationToken cancellationToken)
    {
        if (!await recaptcha.VerifyAsync(request.RecaptchaToken, cancellationToken))
            throw new ValidationException("reCAPTCHA verification failed.");

        if (await db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            throw new InvalidOperationException("An account with this email already exists.");

        if (await db.LawyerLicenses.AnyAsync(l => l.LicenseNumber == request.LicenseNumber, cancellationToken))
            throw new InvalidOperationException("This licence number is already registered.");

        var user = User.CreateLawyer(request.Email, passwordHasher.Hash(request.Password));
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var slug = SlugFrom(request.FullName, user.Id);
        var lawyerProfile = new LawyerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FullName = request.FullName,
            Slug = slug,
            RegionId = request.RegionId,
            CityId = request.CityId,
        };
        db.LawyerProfiles.Add(lawyerProfile);

        db.LawyerLicenses.Add(new LawyerLicense
        {
            Id = Guid.NewGuid(),
            LawyerProfileId = lawyerProfile.Id,
            LicenseNumber = request.LicenseNumber,
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            VerificationStatus = LicenseVerificationStatus.PendingReview,
        });

        var lawyerRole = await db.Roles.SingleAsync(r => r.Name == RoleNames.Lawyer, cancellationToken);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = lawyerRole.Id });

        await db.SaveChangesAsync(cancellationToken);

        var result = await tokenIssuer.IssueAsync(user, cancellationToken);

        await auditLogger.LogAsync("LawyerRegistered", nameof(LawyerProfile), lawyerProfile.Id.ToString(), cancellationToken: cancellationToken);

        return result;
    }

    private static string SlugFrom(string fullName, Guid userId)
    {
        var ascii = new string(fullName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        var suffix = userId.ToString("N")[..8];
        return string.IsNullOrWhiteSpace(ascii) ? $"lawyer-{suffix}" : $"{ascii}-{suffix}";
    }
}
