using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Catalog;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Catalog.Commands;

public record CreateServiceVariantCommand(
    int ServiceId, string NameAr, string NameEn, decimal BasePrice,
    bool RequiresQuantity, int IncludedQuantity, decimal ExtraUnitPrice,
    string? QuantityLabelAr, string? QuantityLabelEn, int MinQuantity, int MaxQuantity, int SortOrder) : IRequest<int>;

public class CreateServiceVariantValidator : AbstractValidator<CreateServiceVariantCommand>
{
    public CreateServiceVariantValidator()
    {
        RuleFor(x => x.ServiceId).GreaterThan(0);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinQuantity).GreaterThan(0);
        RuleFor(x => x.MaxQuantity).GreaterThanOrEqualTo(x => x.MinQuantity);
    }
}

public class CreateServiceVariantHandler(ILawPortalDbContext db) : IRequestHandler<CreateServiceVariantCommand, int>
{
    public async Task<int> Handle(CreateServiceVariantCommand request, CancellationToken cancellationToken)
    {
        var service = await db.ServiceCatalogItems.FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Service not found.");
        if (service.PricingModel != ServicePricingModel.PredefinedCatalog)
            throw new InvalidOperationException("Only a predefined-catalog service takes priced variants.");

        var variant = new ServiceVariant
        {
            ServiceId = request.ServiceId,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            BasePrice = request.BasePrice,
            RequiresQuantity = request.RequiresQuantity,
            IncludedQuantity = request.IncludedQuantity,
            ExtraUnitPrice = request.ExtraUnitPrice,
            QuantityLabelAr = request.QuantityLabelAr,
            QuantityLabelEn = request.QuantityLabelEn,
            MinQuantity = request.MinQuantity,
            MaxQuantity = request.MaxQuantity,
            SortOrder = request.SortOrder,
        };
        db.ServiceVariants.Add(variant);
        await db.SaveChangesAsync(cancellationToken);
        return variant.Id;
    }
}

public record UpdateServiceVariantCommand(
    int Id, string NameAr, string NameEn, decimal BasePrice,
    bool RequiresQuantity, int IncludedQuantity, decimal ExtraUnitPrice,
    string? QuantityLabelAr, string? QuantityLabelEn, int MinQuantity, int MaxQuantity, int SortOrder, bool IsActive) : IRequest<Unit>;

public class UpdateServiceVariantValidator : AbstractValidator<UpdateServiceVariantCommand>
{
    public UpdateServiceVariantValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinQuantity).GreaterThan(0);
        RuleFor(x => x.MaxQuantity).GreaterThanOrEqualTo(x => x.MinQuantity);
    }
}

public class UpdateServiceVariantHandler(ILawPortalDbContext db) : IRequestHandler<UpdateServiceVariantCommand, Unit>
{
    public async Task<Unit> Handle(UpdateServiceVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await db.ServiceVariants.FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Variant not found.");

        variant.NameAr = request.NameAr;
        variant.NameEn = request.NameEn;
        variant.BasePrice = request.BasePrice;
        variant.RequiresQuantity = request.RequiresQuantity;
        variant.IncludedQuantity = request.IncludedQuantity;
        variant.ExtraUnitPrice = request.ExtraUnitPrice;
        variant.QuantityLabelAr = request.QuantityLabelAr;
        variant.QuantityLabelEn = request.QuantityLabelEn;
        variant.MinQuantity = request.MinQuantity;
        variant.MaxQuantity = request.MaxQuantity;
        variant.SortOrder = request.SortOrder;
        variant.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
