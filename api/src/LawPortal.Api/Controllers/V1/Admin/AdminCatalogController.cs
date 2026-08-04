using LawPortal.Application.Admin.Catalog.Commands;
using LawPortal.Application.Admin.Catalog.Dtos;
using LawPortal.Application.Admin.Catalog.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawPortal.Api.Controllers.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/catalog")]
public class AdminCatalogController(ISender sender) : ControllerBase
{
    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<AdminServiceCategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminServiceCategoryDto>>> Categories(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetAdminServiceCategoriesQuery(), cancellationToken));

    [HttpPost("categories")]
    public async Task<ActionResult<int>> CreateCategory(CreateServiceCategoryCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command, cancellationToken));

    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, UpdateServiceCategoryBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateServiceCategoryCommand(id, body.NameAr, body.NameEn, body.IconKey, body.SortOrder, body.IsActive), cancellationToken);
        return NoContent();
    }

    [HttpPost("services")]
    public async Task<ActionResult<int>> CreateService(CreateServiceCatalogItemCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command, cancellationToken));

    [HttpPut("services/{id:int}")]
    public async Task<IActionResult> UpdateService(int id, UpdateServiceCatalogItemBody body, CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateServiceCatalogItemCommand(id, body.NameAr, body.NameEn, body.DescriptionAr, body.DescriptionEn, body.RequiresSpecialty, body.SortOrder, body.IsActive),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("variants")]
    public async Task<ActionResult<int>> CreateVariant(CreateServiceVariantCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command, cancellationToken));

    [HttpPut("variants/{id:int}")]
    public async Task<IActionResult> UpdateVariant(int id, UpdateServiceVariantBody body, CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateServiceVariantCommand(
                id, body.NameAr, body.NameEn, body.BasePrice, body.RequiresQuantity, body.IncludedQuantity, body.ExtraUnitPrice,
                body.QuantityLabelAr, body.QuantityLabelEn, body.MinQuantity, body.MaxQuantity, body.SortOrder, body.IsActive),
            cancellationToken);
        return NoContent();
    }
}

public record UpdateServiceCategoryBody(string NameAr, string NameEn, string? IconKey, int SortOrder, bool IsActive);

public record UpdateServiceCatalogItemBody(
    string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn, bool RequiresSpecialty, int SortOrder, bool IsActive);

public record UpdateServiceVariantBody(
    string NameAr, string NameEn, decimal BasePrice, bool RequiresQuantity, int IncludedQuantity, decimal ExtraUnitPrice,
    string? QuantityLabelAr, string? QuantityLabelEn, int MinQuantity, int MaxQuantity, int SortOrder, bool IsActive);
