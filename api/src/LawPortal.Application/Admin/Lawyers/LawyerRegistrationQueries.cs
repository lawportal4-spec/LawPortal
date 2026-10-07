using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Lawyers;

public record LawyerRegistrationSummaryDto(
    Guid LawyerProfileId,
    string FullName,
    string? PhoneE164,
    string Email,
    string LicenseNumber,
    string Status,
    DateTime SubmittedAtUtc,
    DateTime? ResubmittedAtUtc);

/// <summary>Every lawyer registration an admin can act on, newest first. Sign-ups that never
/// confirmed their phone are left out — they aren't real applicants yet.</summary>
public record ListLawyerRegistrationsQuery(LicenseVerificationStatus? Status, string? Search, int Page, int PageSize)
    : IRequest<PagedResult<LawyerRegistrationSummaryDto>>;

public class ListLawyerRegistrationsHandler(ILawPortalDbContext db)
    : IRequestHandler<ListLawyerRegistrationsQuery, PagedResult<LawyerRegistrationSummaryDto>>
{
    public async Task<PagedResult<LawyerRegistrationSummaryDto>> Handle(ListLawyerRegistrationsQuery request, CancellationToken cancellationToken)
    {
        var query = db.LawyerProfiles
            .Where(l => l.License != null && (l.User!.PhoneE164 == null || l.User.IsPhoneVerified));

        if (request.Status is { } status)
            query = query.Where(l => l.License!.VerificationStatus == status);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(l => l.FullName.Contains(term)
                || l.License!.LicenseNumber.Contains(term)
                || l.User!.Email!.Contains(term)
                || l.User.PhoneE164!.Contains(term));
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.License!.ResubmittedAtUtc ?? l.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LawyerRegistrationSummaryDto(
                l.Id, l.FullName, l.User!.PhoneE164, l.User.Email!, l.License!.LicenseNumber,
                l.License.VerificationStatus.ToString(), l.CreatedAtUtc, l.License.ResubmittedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<LawyerRegistrationSummaryDto>(items, page, pageSize, total);
    }
}

public record LawyerRegistrationDetailDto(
    Guid LawyerProfileId,
    string FullName,
    string? PhoneE164,
    bool IsPhoneVerified,
    string Email,
    string? RegionNameAr,
    string? RegionNameEn,
    string? CityNameAr,
    string? CityNameEn,
    string CountryCode,
    DateTime? TermsAcceptedAtUtc,
    DateTime SubmittedAtUtc,
    string LicenseType,
    string LicenseNumber,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    string Status,
    string? DocumentUrl,
    string? DocumentFileName,
    string? DocumentContentType,
    string? RejectionReason,
    IReadOnlyList<string> CorrectionIssues,
    string? CorrectionNote,
    DateTime? CorrectionRequestedAtUtc,
    DateTime? ResubmittedAtUtc,
    DateTime? DecidedAtUtc,
    /// <summary>Approved but not active yet: "VerifyEmail" or "PayFee" (see LawyerOnboarding).</summary>
    string? OnboardingStep,
    string? PhotoUrl,
    /// <summary>Office / secretary numbers — admin-only, never shown to clients.</summary>
    IReadOnlyList<LawPortal.Application.Lawyers.Account.ContactNumberDto> ContactNumbers);

public record GetLawyerRegistrationQuery(Guid LawyerProfileId) : IRequest<LawyerRegistrationDetailDto>;

public class GetLawyerRegistrationHandler(ILawPortalDbContext db, IFileStorage storage)
    : IRequestHandler<GetLawyerRegistrationQuery, LawyerRegistrationDetailDto>
{
    private static readonly TimeSpan DocumentUrlTtl = TimeSpan.FromMinutes(15);

    public async Task<LawyerRegistrationDetailDto> Handle(GetLawyerRegistrationQuery request, CancellationToken cancellationToken)
    {
        var l = await db.LawyerProfiles
            .Include(p => p.User)
            .Include(p => p.License)
            .Include(p => p.Region)
            .Include(p => p.City)
            .Include(p => p.ContactNumbers)
            .FirstOrDefaultAsync(p => p.Id == request.LawyerProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Lawyer registration not found.");
        var license = l.License ?? throw new KeyNotFoundException("No licence submitted for this lawyer.");

        return new LawyerRegistrationDetailDto(
            l.Id, l.FullName, l.User!.PhoneE164, l.User.IsPhoneVerified, l.User.Email ?? "",
            l.Region?.NameAr, l.Region?.NameEn, l.City?.NameAr, l.City?.NameEn,
            l.CountryCode, l.TermsAcceptedAtUtc, l.CreatedAtUtc,
            license.LicenseType.ToString(), license.LicenseNumber, license.IssueDate, license.ExpiryDate,
            license.VerificationStatus.ToString(),
            license.DocumentStorageKey is null ? null : storage.CreateDownloadUrl(license.DocumentStorageKey, DocumentUrlTtl),
            license.DocumentFileName, license.DocumentContentType,
            license.RejectionReason,
            CorrectionIssueNames.Of(license.CorrectionIssues),
            license.CorrectionNote, license.CorrectionRequestedAtUtc, license.ResubmittedAtUtc,
            license.VerifiedAtUtc,
            LawPortal.Application.Lawyers.Onboarding.LawyerOnboarding.StepOf(l)?.ToString(),
            l.PhotoStorageKey is null ? null : storage.CreateDownloadUrl(l.PhotoStorageKey, DocumentUrlTtl),
            l.ContactNumbers.Select(c => new LawPortal.Application.Lawyers.Account.ContactNumberDto(c.Kind.ToString(), c.ContactName, c.PhoneE164)).ToList());
    }
}

/// <summary>The flags enum as a list of names — what the admin and lawyer UIs key their labels on.</summary>
public static class CorrectionIssueNames
{
    public static IReadOnlyList<string> Of(LicenseCorrectionIssue issues) =>
        Enum.GetValues<LicenseCorrectionIssue>()
            .Where(i => i != LicenseCorrectionIssue.None && issues.HasFlag(i))
            .Select(i => i.ToString())
            .ToList();
}
