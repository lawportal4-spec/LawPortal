using LawPortal.Application.Catalog.Dtos;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Catalog.GetSpecialties;

public record GetSpecialtiesQuery : IRequest<IReadOnlyList<SpecialtyDto>>;

public class GetSpecialtiesQueryHandler(ILawPortalDbContext db)
    : IRequestHandler<GetSpecialtiesQuery, IReadOnlyList<SpecialtyDto>>
{
    public async Task<IReadOnlyList<SpecialtyDto>> Handle(
        GetSpecialtiesQuery request,
        CancellationToken cancellationToken)
    {
        return await db.Specialties
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .Select(s => new SpecialtyDto(
                s.Id,
                s.NameAr,
                s.NameEn,
                s.Slug,
                s.SubSpecialties
                    .Where(sub => sub.IsActive)
                    .Select(sub => new SubSpecialtyDto(sub.Id, sub.NameAr, sub.NameEn))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }
}
