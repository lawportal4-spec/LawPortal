using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Catalog;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Catalog.Commands;

public record CreateServiceCatalogItemCommand(
    int CategoryId, string NameAr, string NameEn, string Slug, string? DescriptionAr, string? DescriptionEn,
    ServicePricingModel PricingModel, bool RequiresSpecialty, int SortOrder) : IRequest<int>;

public class CreateServiceCatalogItemValidator : AbstractValidator<CreateServiceCatalogItemCommand>
{
    public CreateServiceCatalogItemValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(300);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(150).Matches("^[a-z0-9-]+$");
    }
}

public class CreateServiceCatalogItemHandler(ILawPortalDbContext db) : IRequestHandler<CreateServiceCatalogItemCommand, int>
{
    public async Task<int> Handle(CreateServiceCatalogItemCommand request, CancellationToken cancellationToken)
    {
        var categoryExists = await db.ServiceCategories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists) throw new KeyNotFoundException("Category not found.");

        if (await db.ServiceCatalogItems.AnyAsync(s => s.Slug == request.Slug, cancellationToken))
            throw new InvalidOperationException("A service with this slug already exists.");

        var service = new ServiceCatalogItem
        {
            CategoryId = request.CategoryId,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Slug = request.Slug,
            DescriptionAr = request.DescriptionAr,
            DescriptionEn = request.DescriptionEn,
            PricingModel = request.PricingModel,
            RequiresSpecialty = request.RequiresSpecialty,
            SortOrder = request.SortOrder,
        };
        db.ServiceCatalogItems.Add(service);
        await db.SaveChangesAsync(cancellationToken);
        return service.Id;
    }
}

/// <summary>Never changes <see cref="ServiceCatalogItem.PricingModel"/> after creation — every
/// request wizard branches on it (see <c>CreateConsultationDraftHandler</c>,
/// <c>CreateCatalogDraftHandler</c>, <c>CreateBiddingDraftHandler</c>), so flipping it under an
/// already-listed service would silently break whichever flow clients are mid-way through.
/// Retiring a service and creating a fresh one under the right model is the safe path.</summary>
public record UpdateServiceCatalogItemCommand(
    int Id, string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn,
    bool RequiresSpecialty, int SortOrder, bool IsActive) : IRequest<Unit>;

public class UpdateServiceCatalogItemValidator : AbstractValidator<UpdateServiceCatalogItemCommand>
{
    public UpdateServiceCatalogItemValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(300);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(300);
    }
}

public class UpdateServiceCatalogItemHandler(ILawPortalDbContext db) : IRequestHandler<UpdateServiceCatalogItemCommand, Unit>
{
    public async Task<Unit> Handle(UpdateServiceCatalogItemCommand request, CancellationToken cancellationToken)
    {
        var service = await db.ServiceCatalogItems.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Service not found.");

        service.NameAr = request.NameAr;
        service.NameEn = request.NameEn;
        service.DescriptionAr = request.DescriptionAr;
        service.DescriptionEn = request.DescriptionEn;
        service.RequiresSpecialty = request.RequiresSpecialty;
        service.SortOrder = request.SortOrder;
        service.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
