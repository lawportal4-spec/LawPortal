using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Catalog;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Requests.Commands;

/// <summary>Covers both the single-screen predefined-price flow (notarization — variant +
/// quantity) and the details-only flow (trademark — no variant). ServiceVariantId is required
/// for the former, omitted for the latter; the handler checks which one the service actually is.</summary>
public record CreateCatalogDraftCommand(
    int ServiceId,
    int? ServiceVariantId,
    int Quantity,
    string Title,
    string Description) : IRequest<Guid>;

public class CreateCatalogDraftValidator : AbstractValidator<CreateCatalogDraftCommand>
{
    public CreateCatalogDraftValidator()
    {
        RuleFor(x => x.ServiceId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).NotEmpty();
    }
}

public class CreateCatalogDraftHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CreateCatalogDraftCommand, Guid>
{
    public async Task<Guid> Handle(CreateCatalogDraftCommand request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var service = await db.ServiceCatalogItems
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Service not found.");

        if (service.PricingModel is not (ServicePricingModel.PredefinedCatalog or ServicePricingModel.DetailsOnly))
            throw new InvalidOperationException("This service does not use the direct-order flow.");

        decimal? unitPrice = null;
        if (service.PricingModel == ServicePricingModel.PredefinedCatalog)
        {
            if (request.ServiceVariantId is null)
                throw new InvalidOperationException("A variant must be selected for this service.");

            var variant = await db.ServiceVariants
                .FirstOrDefaultAsync(v => v.Id == request.ServiceVariantId && v.ServiceId == service.Id && v.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Variant not found.");

            if (request.Quantity < variant.MinQuantity || request.Quantity > variant.MaxQuantity)
                throw new InvalidOperationException($"Quantity must be between {variant.MinQuantity} and {variant.MaxQuantity}.");

            unitPrice = variant.PriceFor(request.Quantity);
        }

        var draft = new CatalogRequest
        {
            Id = Guid.NewGuid(),
            Number = await RequestNumberGenerator.NextAsync(db, cancellationToken),
            ClientId = clientId,
            ServiceId = service.Id,
            ServiceVariantId = request.ServiceVariantId,
            Quantity = request.Quantity,
            UnitPriceSnapshot = unitPrice,
            Subtotal = unitPrice,
            Title = request.Title,
            Description = request.Description,
        };

        db.CatalogRequests.Add(draft);
        await db.SaveChangesAsync(cancellationToken);
        return draft.Id;
    }
}
