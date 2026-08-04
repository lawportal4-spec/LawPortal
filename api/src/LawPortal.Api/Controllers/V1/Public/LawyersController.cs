using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Application.Lawyers.Queries;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Public;

[ApiController]
[Route("api/v1/lawyers")]
public class LawyersController(ISender sender) : ControllerBase
{
    /// <summary>Faceted lawyer-directory search — specialty/city/region/language/gender
    /// filters, a name search, 6 sort options, and pagination.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<LawyerCardDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LawyerCardDto>>> Search(
        [FromQuery] int? specialtyId,
        [FromQuery] int? cityId,
        [FromQuery] int? regionId,
        [FromQuery] int? languageId,
        [FromQuery] Gender? gender,
        [FromQuery] string? q,
        [FromQuery] LawyerSortOption sort = LawyerSortOption.Newest,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new SearchLawyersQuery(specialtyId, cityId, regionId, languageId, gender, q, sort, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Full public profile — bio, specialties, languages, qualifications,
    /// pricing matrix, and the credential (licence) data.</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType<LawyerProfileDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LawyerProfileDetailDto>> GetProfile(string slug, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLawyerProfileQuery(slug), cancellationToken);
        return Ok(result);
    }
}
