using LawPortal.Application.Catalog.Dtos;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Catalog.Queries;

public record GetServiceCategoriesQuery : IRequest<IReadOnlyList<ServiceCategoryDto>>;

public class GetServiceCategoriesHandler(ILawPortalDbContext db)
    : IRequestHandler<GetServiceCategoriesQuery, IReadOnlyList<ServiceCategoryDto>>
{
    public async Task<IReadOnlyList<ServiceCategoryDto>> Handle(GetServiceCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await db.ServiceCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new ServiceCategoryDto(
                c.Id,
                c.NameAr,
                c.NameEn,
                c.Slug,
                c.IconKey,
                c.Services
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new ServiceCatalogItemDto(s.Id, s.NameAr, s.NameEn, s.Slug, s.DescriptionAr, s.DescriptionEn, s.PricingModel.ToString(), s.RequiresSpecialty))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }
}
