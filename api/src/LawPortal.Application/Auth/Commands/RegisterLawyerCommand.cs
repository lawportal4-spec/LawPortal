using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Auth.Commands;

/// <summary>The licence scan as received by the API. Small enough (3MB cap) to buffer for the
/// virus scan before it is stored.</summary>
public record LicenseDocumentUpload(Stream Content, string FileName, string ContentType, long Length);

/// <summary>The lawyer registration wizard's three steps (personal data → region/city → licence)
/// submit together here. The account is created unverified and a phone OTP is sent; sign-in is
/// refused until <see cref="VerifyLawyerRegistrationCommand"/> confirms it. Submitting again
/// before that — e.g. after "edit phone number" — updates the same pending account instead of
/// tripping over its own email or phone.</summary>
public record RegisterLawyerCommand(
    string FullName,
    string PhoneE164,
    string Email,
    string Password,
    int RegionId,
    int CityId,
    LawyerLicenseType LicenseType,
    string LicenseNumber,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    string CountryCode,
    bool AcceptedTerms,
    LicenseDocumentUpload? LicenseDocument,
    string RecaptchaToken) : IRequest<LawyerRegistrationStartedDto>;

public record LawyerRegistrationStartedDto(string PhoneE164);

public class RegisterLawyerValidator : AbstractValidator<RegisterLawyerCommand>
{
    public const long MaxDocumentBytes = 3 * 1024 * 1024;

    public static readonly string[] AllowedDocumentTypes =
        ["image/jpeg", "image/png", "image/jpg", "image/webp", "application/pdf"];

    public RegisterLawyerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneE164).Matches(@"^\+9665\d{8}$")
            .WithMessage("Phone must be a Saudi mobile number in E.164 format, e.g. +966501234567.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).LawyerPassword();
        RuleFor(x => x.RegionId).GreaterThan(0);
        RuleFor(x => x.CityId).GreaterThan(0);
        RuleFor(x => x.LicenseType).IsInEnum();
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ExpiryDate).GreaterThan(x => x.IssueDate);
        RuleFor(x => x.CountryCode).Equal("SA").WithMessage("Only Saudi Arabia is supported.");
        RuleFor(x => x.AcceptedTerms).Equal(true).WithMessage("You must accept the terms and privacy policy.");
        RuleFor(x => x.LicenseDocument).NotNull().WithMessage("The licence document is required.");
        RuleFor(x => x.LicenseDocument!.Length).InclusiveBetween(1, MaxDocumentBytes)
            .WithMessage("The licence document must be at most 3MB.")
            .When(x => x.LicenseDocument is not null);
        RuleFor(x => x.LicenseDocument!.ContentType).Must(t => AllowedDocumentTypes.Contains(t))
            .WithMessage("The licence document must be an image (jpeg, png, webp) or a PDF.")
            .When(x => x.LicenseDocument is not null);
        RuleFor(x => x.RecaptchaToken).NotEmpty();
    }
}

public static class LawyerPasswordRule
{
    /// <summary>Same rule at registration and password reset.</summary>
    public static IRuleBuilderOptions<T, string> LawyerPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain a digit.");
}

public class RegisterLawyerHandler(
    ILawPortalDbContext db,
    IPasswordHasher passwordHasher,
    IRecaptchaVerifier recaptcha,
    IFileStorage storage,
    IVirusScanner scanner,
    OtpService otpService,
    IAuditLogger auditLogger)
    : IRequestHandler<RegisterLawyerCommand, LawyerRegistrationStartedDto>
{
    public async Task<LawyerRegistrationStartedDto> Handle(RegisterLawyerCommand request, CancellationToken cancellationToken)
    {
        if (!await recaptcha.VerifyAsync(request.RecaptchaToken, cancellationToken))
            throw new ValidationException("reCAPTCHA verification failed.");

        if (!await db.Cities.AnyAsync(c => c.Id == request.CityId && c.RegionId == request.RegionId, cancellationToken))
            throw new ValidationException("The selected city is not in the selected region.");

        // An earlier, never-activated attempt by the same person: reuse it rather than block them.
        var pending = await db.Users
            .Include(u => u.LawyerProfile).ThenInclude(p => p!.License)
            .FirstOrDefaultAsync(u => u.UserType == UserType.Lawyer && !u.IsPhoneVerified
                && (u.Email == request.Email || u.PhoneE164 == request.PhoneE164), cancellationToken);
        var pendingId = pending?.Id;

        if (await db.Users.AnyAsync(u => u.Id != pendingId && u.Email == request.Email, cancellationToken))
            throw new InvalidOperationException("An account with this email already exists.");

        if (await db.Users.AnyAsync(u => u.Id != pendingId && u.PhoneE164 == request.PhoneE164, cancellationToken))
            throw new InvalidOperationException("An account with this phone number already exists.");

        if (await db.LawyerLicenses.AnyAsync(
                l => l.LicenseNumber == request.LicenseNumber && l.LawyerProfile!.UserId != pendingId, cancellationToken))
            throw new InvalidOperationException("This licence number is already registered.");

        var document = request.LicenseDocument!;
        var storageKey = await ScanAndStoreAsync(document, cancellationToken);

        var user = pending ?? User.CreateLawyer(request.Email, request.PhoneE164, string.Empty);
        user.Email = request.Email;
        user.PhoneE164 = request.PhoneE164;
        user.PasswordHash = passwordHasher.Hash(request.Password);

        if (pending is null)
        {
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken); // need the generated Id before adding profile/role FK rows

            var lawyerRole = await db.Roles.SingleAsync(r => r.Name == RoleNames.Lawyer, cancellationToken);
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = lawyerRole.Id });
        }

        var profile = user.LawyerProfile;
        if (profile is null)
        {
            profile = new LawyerProfile { Id = Guid.NewGuid(), UserId = user.Id, FullName = request.FullName, Slug = SlugFrom(request.FullName, user.Id) };
            db.LawyerProfiles.Add(profile);
        }
        profile.FullName = request.FullName;
        profile.RegionId = request.RegionId;
        profile.CityId = request.CityId;
        profile.CountryCode = request.CountryCode;
        profile.TermsAcceptedAtUtc = DateTime.UtcNow;

        var license = profile.License;
        if (license is null)
        {
            license = new LawyerLicense { Id = Guid.NewGuid(), LawyerProfileId = profile.Id, LicenseNumber = request.LicenseNumber };
            db.LawyerLicenses.Add(license);
        }
        license.LicenseNumber = request.LicenseNumber;
        license.LicenseType = request.LicenseType;
        license.IssueDate = request.IssueDate;
        license.ExpiryDate = request.ExpiryDate;
        license.VerificationStatus = LicenseVerificationStatus.PendingReview;
        license.DocumentStorageKey = storageKey;
        license.DocumentFileName = document.FileName;
        license.DocumentContentType = document.ContentType;

        await db.SaveChangesAsync(cancellationToken);

        await otpService.IssueAsync(request.PhoneE164, OtpPurpose.LawyerRegistration, cancellationToken);

        await auditLogger.LogAsync("LawyerRegistered", nameof(LawyerProfile), profile.Id.ToString(), cancellationToken: cancellationToken);

        return new LawyerRegistrationStartedDto(request.PhoneE164);
    }

    private async Task<string> ScanAndStoreAsync(LicenseDocumentUpload document, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await document.Content.CopyToAsync(buffer, cancellationToken);

        buffer.Position = 0;
        var scan = await scanner.ScanAsync(buffer, cancellationToken);
        // Same policy as request attachments: only a positive detection blocks. An unreachable
        // scanner (ScanFailed) is not the uploader's fault and the admin reviews the file anyway.
        if (scan.Outcome == ScanOutcome.Infected)
            throw new InvalidOperationException("This file failed a security scan and was rejected.");

        buffer.Position = 0;
        return await storage.UploadAsync(Path.GetFileName(document.FileName), document.ContentType, buffer, cancellationToken);
    }

    private static string SlugFrom(string fullName, Guid userId)
    {
        var ascii = new string(fullName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        var suffix = userId.ToString("N")[..8];
        return string.IsNullOrWhiteSpace(ascii) ? $"lawyer-{suffix}" : $"{ascii}-{suffix}";
    }
}
