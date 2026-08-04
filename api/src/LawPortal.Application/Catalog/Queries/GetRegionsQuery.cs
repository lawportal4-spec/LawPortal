using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Catalog.Queries;

public record CityDto(int Id, string NameAr, string NameEn);

public record RegionDto(int Id, string NameAr, string NameEn, IReadOnlyList<CityDto> Cities);

/// <summary>Region/city lookup for the lawyer registration form — step 2 of the documented
/// 3-step wizard (P1's own progress log). Never exposed until now because no frontend
/// registration screen existed to consume it; the backend command (<c>RegisterLawyerCommand</c>)
/// has accepted these ids since P1.</summary>
public record GetRegionsQuery : IRequest<IReadOnlyList<RegionDto>>;

public class GetRegionsHandler(ILawPortalDbContext db) : IRequestHandler<GetRegionsQuery, IReadOnlyList<RegionDto>>
{
    public async Task<IReadOnlyList<RegionDto>> Handle(GetRegionsQuery request, CancellationToken cancellationToken)
    {
        var regions = await db.Regions
            .Where(r => r.IsActive)
            .Include(r => r.Cities.Where(c => c.IsActive))
            .ToListAsync(cancellationToken);

        return regions.Select(r => new RegionDto(r.Id, r.NameAr, r.NameEn, r.Cities.Select(c => new CityDto(c.Id, c.NameAr, c.NameEn)).ToList())).ToList();
    }
}
