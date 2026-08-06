using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Queries;

public record GetLawyerProfileQuery(string Slug) : IRequest<LawyerProfileDetailDto>;

public class GetLawyerProfileHandler(ILawPortalDbContext db) : IRequestHandler<GetLawyerProfileQuery, LawyerProfileDetailDto>
{
    public async Task<LawyerProfileDetailDto> Handle(GetLawyerProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await db.LawyerProfiles
            .Where(l => l.Slug == request.Slug && l.IsVerified)
            .Select(l => new LawyerProfileDetailDto(
                l.Id,
                l.Slug,
                l.FullName,
                l.BioAr,
                l.BioEn,
                l.Gender != null ? l.Gender.ToString() : null,
                l.City != null ? l.City.NameAr : null,
                l.City != null ? l.City.NameEn : null,
                l.Region != null ? l.Region.NameAr : null,
                l.Region != null ? l.Region.NameEn : null,
                l.ExperienceRange != null
                    ? (l.ExperienceRange.MaxYears.HasValue
                        ? l.ExperienceRange.MinYears + "-" + l.ExperienceRange.MaxYears
                        : l.ExperienceRange.MinYears + "+")
                    : null,
                l.IsVerified,
                l.IsVatRegistered,
                l.AcceptingNewRequests,
                l.AvgRating,
                l.RatingCount,
                l.CompletedRequestCount,
                new LawyerLicenseDto(l.License!.LicenseNumber, l.License.IssueDate, l.License.ExpiryDate),
                l.Pricing != null
                    ? new LawyerPricingDto(l.Pricing.WrittenPrice, l.Pricing.Price15, l.Pricing.Price30, l.Pricing.Price45, l.Pricing.PriceIsVatInclusive)
                    : null,
                l.LawyerSpecialties.Select(ls => ls.Specialty!.NameAr).ToList(),
                l.LawyerSpecialties.Select(ls => ls.Specialty!.NameEn).ToList(),
                l.LawyerSpecialties.Select(ls => new LawyerSpecialtyDto(ls.Specialty!.Id, ls.Specialty.NameAr, ls.Specialty.NameEn)).ToList(),
                l.LawyerLanguages.Select(ll => ll.Language!.NameAr).ToList(),
                l.LawyerLanguages.Select(ll => ll.Language!.NameEn).ToList(),
                l.Qualifications
                    .OrderBy(q => q.SortOrder)
                    .Select(q => new LawyerQualificationDto(q.Kind.ToString(), q.TitleAr, q.TitleEn, q.Institution, q.FromYear, q.ToYear))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return profile ?? throw new KeyNotFoundException("Lawyer profile not found.");
    }
}
