using LawPortal.Application.Catalog.Dtos;
using LawPortal.Application.Catalog.GetSpecialties;
using LawPortal.Application.Catalog.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Shared;

[ApiController]
[Route("api/v1/catalog")]
public class CatalogController(ISender sender) : ControllerBase
{
    /// <summary>The 13 legal specialties shared across every service category.</summary>
    [HttpGet("specialties")]
    [ProducesResponseType<IReadOnlyList<SpecialtyDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SpecialtyDto>>> GetSpecialties(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSpecialtiesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>The 5 service categories, each with its specific services.</summary>
    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<ServiceCategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServiceCategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetServiceCategoriesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Priced variants for a PredefinedCatalog service (e.g. "وكالة شركات" vs
    /// "وكالة فردية" under "إنشاء وكالة").</summary>
    [HttpGet("services/{serviceId:int}/variants")]
    [ProducesResponseType<IReadOnlyList<ServiceVariantDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServiceVariantDto>>> GetVariants(int serviceId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetServiceVariantsQuery(serviceId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("languages")]
    [ProducesResponseType<IReadOnlyList<LawPortal.Application.Catalog.Queries.LanguageDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LawPortal.Application.Catalog.Queries.LanguageDto>>> GetLanguages(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LawPortal.Application.Catalog.Queries.GetLanguagesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Regions with their cities, for the lawyer registration form.</summary>
    [HttpGet("regions")]
    [ProducesResponseType<IReadOnlyList<LawPortal.Application.Catalog.Queries.RegionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LawPortal.Application.Catalog.Queries.RegionDto>>> GetRegions(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LawPortal.Application.Catalog.Queries.GetRegionsQuery(), cancellationToken);
        return Ok(result);
    }
}
