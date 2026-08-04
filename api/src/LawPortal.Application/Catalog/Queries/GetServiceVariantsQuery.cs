using LawPortal.Application.Catalog.Dtos;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Catalog.Queries;

public record GetServiceVariantsQuery(int ServiceId) : IRequest<IReadOnlyList<ServiceVariantDto>>;

public class GetServiceVariantsHandler(ILawPortalDbContext db)
    : IRequestHandler<GetServiceVariantsQuery, IReadOnlyList<ServiceVariantDto>>
{
    public async Task<IReadOnlyList<ServiceVariantDto>> Handle(GetServiceVariantsQuery request, CancellationToken cancellationToken)
    {
        return await db.ServiceVariants
            .Where(v => v.ServiceId == request.ServiceId && v.IsActive)
            .OrderBy(v => v.SortOrder)
            .Select(v => new ServiceVariantDto(
                v.Id, v.NameAr, v.NameEn, v.BasePrice, v.RequiresQuantity,
                v.IncludedQuantity, v.ExtraUnitPrice, v.QuantityLabelAr, v.QuantityLabelEn,
                v.MinQuantity, v.MaxQuantity))
            .ToListAsync(cancellationToken);
    }
}
