using LawPortal.Application.Admin.Catalog.Dtos;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Catalog.Queries;

/// <summary>The client-facing <c>GetServiceCategoriesQuery</c> only ever shows active rows to a
/// browsing client — this is the CMS view: everything, including what's currently hidden, so an
/// admin can see what they're about to reactivate or already turned off.</summary>
public record GetAdminServiceCategoriesQuery : IRequest<IReadOnlyList<AdminServiceCategoryDto>>;

public class GetAdminServiceCategoriesHandler(ILawPortalDbContext db)
    : IRequestHandler<GetAdminServiceCategoriesQuery, IReadOnlyList<AdminServiceCategoryDto>>
{
    public async Task<IReadOnlyList<AdminServiceCategoryDto>> Handle(GetAdminServiceCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await db.ServiceCategories
            .Include(c => c.Services).ThenInclude(s => s.Variants)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

        return categories.Select(c => new AdminServiceCategoryDto(
            c.Id, c.NameAr, c.NameEn, c.Slug, c.IconKey, c.SortOrder, c.IsActive,
            c.Services.OrderBy(s => s.SortOrder).Select(s => new AdminServiceCatalogItemDto(
                s.Id, s.CategoryId, s.NameAr, s.NameEn, s.Slug, s.DescriptionAr, s.DescriptionEn,
                s.PricingModel.ToString(), s.RequiresSpecialty, s.SortOrder, s.IsActive,
                s.Variants.OrderBy(v => v.SortOrder).Select(v => new AdminServiceVariantDto(
                    v.Id, v.ServiceId, v.NameAr, v.NameEn, v.BasePrice, v.RequiresQuantity, v.IncludedQuantity,
                    v.ExtraUnitPrice, v.QuantityLabelAr, v.QuantityLabelEn, v.MinQuantity, v.MaxQuantity,
                    v.SortOrder, v.IsActive)).ToList()
            )).ToList()
        )).ToList();
    }
}
