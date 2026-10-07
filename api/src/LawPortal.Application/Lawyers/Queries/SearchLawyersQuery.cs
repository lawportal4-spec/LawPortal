using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Queries;

public record SearchLawyersQuery(
    int? SpecialtyId,
    int? CityId,
    int? RegionId,
    int? LanguageId,
    Gender? Gender,
    string? Query,
    LawyerSortOption Sort,
    int Page,
    int PageSize) : IRequest<PagedResult<LawyerCardDto>>;

public class SearchLawyersHandler(ILawPortalDbContext db, IFileStorage storage) : IRequestHandler<SearchLawyersQuery, PagedResult<LawyerCardDto>>
{
    internal static readonly TimeSpan PhotoUrlTtl = TimeSpan.FromHours(6);

    public async Task<PagedResult<LawyerCardDto>> Handle(SearchLawyersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        // A verified lawyer with no published price yet can't be booked — keep them out of
        // the directory rather than crash the projection below on a null Pricing/License.
        var query = db.LawyerProfiles.Where(l => l.IsVerified && l.Pricing != null && l.License != null);

        if (request.SpecialtyId is { } specialtyId)
            query = query.Where(l => l.LawyerSpecialties.Any(ls => ls.SpecialtyId == specialtyId));

        if (request.CityId is { } cityId)
            query = query.Where(l => l.CityId == cityId);

        if (request.RegionId is { } regionId)
            query = query.Where(l => l.RegionId == regionId);

        if (request.LanguageId is { } languageId)
            query = query.Where(l => l.LawyerLanguages.Any(ll => ll.LanguageId == languageId));

        if (request.Gender is { } gender)
            query = query.Where(l => l.Gender == gender);

        if (!string.IsNullOrWhiteSpace(request.Query))
            query = query.Where(l => l.FullName.Contains(request.Query));

        query = request.Sort switch
        {
            LawyerSortOption.Rating => query.OrderByDescending(l => l.AvgRating),
            LawyerSortOption.City => query.OrderBy(l => l.City!.NameAr),
            LawyerSortOption.Experience => query.OrderByDescending(l => l.ExperienceRange!.MinYears),
            LawyerSortOption.Price => query.OrderBy(l => l.Pricing!.WrittenPrice),
            LawyerSortOption.MostRequested => query.OrderByDescending(l => l.CompletedRequestCount),
            _ => query.OrderByDescending(l => l.CreatedAtUtc),
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LawyerCardDto(
                l.Id,
                l.Slug,
                l.FullName,
                l.IsVerified,
                l.City!.NameAr,
                l.City.NameEn,
                l.AvgRating,
                l.RatingCount,
                l.CompletedRequestCount,
                l.ExperienceRange != null
                    ? (l.ExperienceRange.MaxYears.HasValue
                        ? l.ExperienceRange.MinYears + "-" + l.ExperienceRange.MaxYears
                        : l.ExperienceRange.MinYears + "+")
                    : null,
                l.License!.LicenseNumber,
                l.Pricing!.WrittenPrice,
                l.IsVatRegistered,
                l.LawyerSpecialties.Select(ls => ls.Specialty!.NameAr).ToList(),
                l.LawyerSpecialties.Select(ls => ls.Specialty!.NameEn).ToList(),
                l.PhotoStorageKey))
            .ToListAsync(cancellationToken);

        // The projection carries the storage key; swap it for a signed URL here, outside the SQL.
        items = items.Select(i => i with { PhotoUrl = i.PhotoUrl is null ? null : storage.CreateDownloadUrl(i.PhotoUrl, PhotoUrlTtl) }).ToList();
        return new PagedResult<LawyerCardDto>(items, page, pageSize, totalCount);
    }
}
