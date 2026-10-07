using System.Net;
using FluentValidation;
using LawPortal.Application.Auth;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Lawyers.Onboarding;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LawPortal.Application.Lawyers.Account;

/// <summary>The lawyer's own personal details ("البيانات الشخصية"): photo, sign-in mobile and email,
/// location, and the extra contact numbers only admins see.</summary>
public record LawyerAccountDto(
    string FullName,
    string? PhotoUrl,
    string? PhoneE164,
    string? Email,
    /// <summary>An address waiting for its confirmation link to be followed.</summary>
    string? PendingEmail,
    int? RegionId,
    int? CityId,
    IReadOnlyList<ContactNumberDto> ContactNumbers);

public record ContactNumberDto(string Kind, string? ContactName, string PhoneE164);

internal static class LawyerAccounts
{
    public static readonly TimeSpan PhotoUrlTtl = TimeSpan.FromHours(6);
    public const long MaxPhotoBytes = 2 * 1024 * 1024;
    public static readonly string[] PhotoTypes = ["image/jpeg", "image/png", "image/jpg"];
    public const string SaudiMobile = @"^\+9665\d{8}$";
    /// <summary>Mobile, landline (+966 1x…), 800 and 9200 numbers.</summary>
    public const string SaudiAnyNumber = @"^\+966\d{8,10}$";

    public static async Task<LawyerProfile> LoadAsync(ILawPortalDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var id = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        return await db.LawyerProfiles.Include(l => l.User).Include(l => l.ContactNumbers).FirstAsync(l => l.Id == id, cancellationToken);
    }
}

public record GetLawyerAccountQuery : IRequest<LawyerAccountDto>;

public class GetLawyerAccountHandler(ILawPortalDbContext db, ICurrentUser currentUser, IFileStorage storage)
    : IRequestHandler<GetLawyerAccountQuery, LawyerAccountDto>
{
    public async Task<LawyerAccountDto> Handle(GetLawyerAccountQuery request, CancellationToken cancellationToken)
    {
        var lawyer = await LawyerAccounts.LoadAsync(db, currentUser, cancellationToken);
        var now = DateTime.UtcNow;
        var pendingEmail = await db.EmailVerificationTokens
            .Where(t => t.UserId == lawyer.UserId && t.NewEmail != null && t.UsedAtUtc == null && t.ExpiresAtUtc > now)
            .Select(t => t.NewEmail)
            .FirstOrDefaultAsync(cancellationToken);

        return new LawyerAccountDto(
            lawyer.FullName,
            lawyer.PhotoStorageKey is null ? null : storage.CreateDownloadUrl(lawyer.PhotoStorageKey, LawyerAccounts.PhotoUrlTtl),
            lawyer.User!.PhoneE164,
            lawyer.User.Email,
            pendingEmail,
            lawyer.RegionId,
            lawyer.CityId,
            lawyer.ContactNumbers.Select(c => new ContactNumberDto(c.Kind.ToString(), c.ContactName, c.PhoneE164)).ToList());
    }
}

// ── Photo ────────────────────────────────────────────────────────────────────────────────────

public record PhotoUpload(Stream Content, string FileName, string ContentType, long Length);

public record SetLawyerPhotoCommand(PhotoUpload? Photo) : IRequest<string?>;

public class SetLawyerPhotoValidator : AbstractValidator<SetLawyerPhotoCommand>
{
    public SetLawyerPhotoValidator()
    {
        RuleFor(x => x.Photo!.Length).InclusiveBetween(1, LawyerAccounts.MaxPhotoBytes)
            .WithMessage("The photo must be at most 2MB.").When(x => x.Photo is not null);
        RuleFor(x => x.Photo!.ContentType).Must(t => LawyerAccounts.PhotoTypes.Contains(t))
            .WithMessage("The photo must be a JPG or PNG image.").When(x => x.Photo is not null);
    }
}

/// <summary>Replaces the profile photo, or removes it when <see cref="SetLawyerPhotoCommand.Photo"/> is null.
/// Returns the new photo's URL.</summary>
public class SetLawyerPhotoHandler(ILawPortalDbContext db, ICurrentUser currentUser, IFileStorage storage, IAuditLogger auditLogger)
    : IRequestHandler<SetLawyerPhotoCommand, string?>
{
    public async Task<string?> Handle(SetLawyerPhotoCommand request, CancellationToken cancellationToken)
    {
        var lawyer = await LawyerAccounts.LoadAsync(db, currentUser, cancellationToken);
        // ponytail: the replaced file stays in the bucket; add a delete once storage costs matter.
        lawyer.PhotoStorageKey = request.Photo is { } photo
            ? await storage.UploadAsync(Path.GetFileName(photo.FileName), photo.ContentType, photo.Content, cancellationToken)
            : null;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(request.Photo is null ? "LawyerPhotoRemoved" : "LawyerPhotoChanged", nameof(LawyerProfile), lawyer.Id.ToString(), cancellationToken: cancellationToken);
        return lawyer.PhotoStorageKey is null ? null : storage.CreateDownloadUrl(lawyer.PhotoStorageKey, LawyerAccounts.PhotoUrlTtl);
    }
}

// ── Sign-in mobile: code to the new number, then switch ─────────────────────────────────────

public record RequestLawyerPhoneChangeCommand(string PhoneE164) : IRequest<Unit>;

public class RequestLawyerPhoneChangeValidator : AbstractValidator<RequestLawyerPhoneChangeCommand>
{
    public RequestLawyerPhoneChangeValidator() => RuleFor(x => x.PhoneE164).Matches(LawyerAccounts.SaudiMobile);
}

public class RequestLawyerPhoneChangeHandler(ILawPortalDbContext db, ICurrentUser currentUser, OtpService otpService)
    : IRequestHandler<RequestLawyerPhoneChangeCommand, Unit>
{
    public async Task<Unit> Handle(RequestLawyerPhoneChangeCommand request, CancellationToken cancellationToken)
    {
        var lawyer = await LawyerAccounts.LoadAsync(db, currentUser, cancellationToken);
        if (lawyer.User!.PhoneE164 == request.PhoneE164) throw new InvalidOperationException("This is already your mobile number.");
        if (await db.Users.AnyAsync(u => u.PhoneE164 == request.PhoneE164, cancellationToken))
            throw new InvalidOperationException("This mobile number is already used by another account.");
        await otpService.IssueAsync(request.PhoneE164, OtpPurpose.ChangePhone, cancellationToken);
        return Unit.Value;
    }
}

public record ConfirmLawyerPhoneChangeCommand(string PhoneE164, string Code) : IRequest<Unit>;

public class ConfirmLawyerPhoneChangeValidator : AbstractValidator<ConfirmLawyerPhoneChangeCommand>
{
    public ConfirmLawyerPhoneChangeValidator()
    {
        RuleFor(x => x.PhoneE164).Matches(LawyerAccounts.SaudiMobile);
        RuleFor(x => x.Code).Matches(@"^\d{6}$");
    }
}

public class ConfirmLawyerPhoneChangeHandler(ILawPortalDbContext db, ICurrentUser currentUser, OtpService otpService, IAuditLogger auditLogger)
    : IRequestHandler<ConfirmLawyerPhoneChangeCommand, Unit>
{
    public async Task<Unit> Handle(ConfirmLawyerPhoneChangeCommand request, CancellationToken cancellationToken)
    {
        var lawyer = await LawyerAccounts.LoadAsync(db, currentUser, cancellationToken);
        await otpService.VerifyAsync(request.PhoneE164, request.Code, OtpPurpose.ChangePhone, cancellationToken);
        // Checked again: someone may have registered the number while the code was in flight.
        if (await db.Users.AnyAsync(u => u.Id != lawyer.UserId && u.PhoneE164 == request.PhoneE164, cancellationToken))
            throw new InvalidOperationException("This mobile number is already used by another account.");

        var old = lawyer.User!.PhoneE164;
        lawyer.User.PhoneE164 = request.PhoneE164;
        lawyer.User.IsPhoneVerified = true;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("LawyerPhoneChanged", nameof(User), lawyer.UserId.ToString(), $"{old} -> {request.PhoneE164}", cancellationToken);
        return Unit.Value;
    }
}

// ── Sign-in email: link to the new address; it switches when followed ─────────────────────

public record RequestLawyerEmailChangeCommand(string Email) : IRequest<Unit>;

public class RequestLawyerEmailChangeValidator : AbstractValidator<RequestLawyerEmailChangeCommand>
{
    public RequestLawyerEmailChangeValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
}

public class RequestLawyerEmailChangeHandler(
    ILawPortalDbContext db, ICurrentUser currentUser, ITokenService tokenService, IEmailSender emailSender, IConfiguration configuration)
    : IRequestHandler<RequestLawyerEmailChangeCommand, Unit>
{
    public async Task<Unit> Handle(RequestLawyerEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var lawyer = await LawyerAccounts.LoadAsync(db, currentUser, cancellationToken);
        var email = request.Email.Trim();
        if (string.Equals(lawyer.User!.Email, email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This is already your email address.");
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            throw new InvalidOperationException("This email is already used by another account.");

        var link = await LawyerOnboarding.IssueEmailLinkAsync(db, tokenService, configuration, lawyer.UserId, email, cancellationToken);
        var name = WebUtility.HtmlEncode(lawyer.FullName);
        var body = $"""
            <div dir="rtl" style="font-family: Tahoma, Arial, sans-serif; max-width: 560px; margin: auto; color: #1f2933; line-height: 1.8;">
              <h2 style="color: #0f5c45;">تأكيد البريد الإلكتروني الجديد</h2>
              <p>مرحبًا أ. {name}،</p>
              <p>طلبت تغيير بريدك الإلكتروني في بوابة القانون إلى هذا العنوان. لتأكيد التغيير، يُرجى الضغط على الزر أدناه:</p>
              <p style="text-align: center; margin: 28px 0;">
                <a href="{link}" style="background: #0f5c45; color: #fff; padding: 12px 32px; border-radius: 8px; text-decoration: none; font-weight: bold;">تأكيد البريد الإلكتروني</a>
              </p>
              <p style="font-size: 13px; color: #6b7280;">ينتهي هذا الرابط خلال 72 ساعة. إذا لم تطلب هذا التغيير فتجاهل هذه الرسالة، وسيبقى بريدك الحالي كما هو.</p>
            </div>
            """;
        await emailSender.SendAsync(email, "تأكيد البريد الإلكتروني الجديد في بوابة القانون", body, cancellationToken);
        return Unit.Value;
    }
}

// ── Location ─────────────────────────────────────────────────────────────────────────────────

public record UpdateLawyerLocationCommand(int RegionId, int CityId) : IRequest<Unit>;

public class UpdateLawyerLocationHandler(ILawPortalDbContext db, ICurrentUser currentUser, IAuditLogger auditLogger)
    : IRequestHandler<UpdateLawyerLocationCommand, Unit>
{
    public async Task<Unit> Handle(UpdateLawyerLocationCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Cities.AnyAsync(c => c.Id == request.CityId && c.RegionId == request.RegionId, cancellationToken))
            throw new InvalidOperationException("The city does not belong to the selected region.");

        var lawyer = await LawyerAccounts.LoadAsync(db, currentUser, cancellationToken);
        var old = $"{lawyer.RegionId}/{lawyer.CityId}";
        lawyer.RegionId = request.RegionId;
        lawyer.CityId = request.CityId;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("LawyerLocationChanged", nameof(LawyerProfile), lawyer.Id.ToString(), $"{old} -> {request.RegionId}/{request.CityId}", cancellationToken);
        return Unit.Value;
    }
}

// ── Extra contact numbers (admin-only) ──────────────────────────────────────────────────────

public record ContactNumberInput(ContactNumberKind Kind, string? ContactName, string PhoneE164);

/// <summary>Replaces the whole list — the page edits it as one form.</summary>
public record SaveLawyerContactNumbersCommand(IReadOnlyList<ContactNumberInput> Numbers) : IRequest<Unit>;

public class SaveLawyerContactNumbersValidator : AbstractValidator<SaveLawyerContactNumbersCommand>
{
    public SaveLawyerContactNumbersValidator()
    {
        RuleFor(x => x.Numbers).Must(n => n.Count <= 5).WithMessage("At most 5 contact numbers.");
        RuleForEach(x => x.Numbers).ChildRules(n =>
        {
            n.RuleFor(c => c.Kind).IsInEnum();
            n.RuleFor(c => c.ContactName).MaximumLength(100);
            n.RuleFor(c => c.PhoneE164).Matches(LawyerAccounts.SaudiAnyNumber);
        });
    }
}

public class SaveLawyerContactNumbersHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<SaveLawyerContactNumbersCommand, Unit>
{
    public async Task<Unit> Handle(SaveLawyerContactNumbersCommand request, CancellationToken cancellationToken)
    {
        var lawyer = await LawyerAccounts.LoadAsync(db, currentUser, cancellationToken);
        db.LawyerContactNumbers.RemoveRange(lawyer.ContactNumbers);
        foreach (var n in request.Numbers)
        {
            db.LawyerContactNumbers.Add(new LawyerContactNumber
            {
                Id = Guid.NewGuid(),
                LawyerProfileId = lawyer.Id,
                Kind = n.Kind,
                ContactName = string.IsNullOrWhiteSpace(n.ContactName) ? null : n.ContactName.Trim(),
                PhoneE164 = n.PhoneE164,
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
