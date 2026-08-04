using LawPortal.Domain.Catalog;

namespace LawPortal.Domain.Requests;

/// <summary>Covers both predefined-catalog-price services (notarization — variant + quantity
/// selected, single screen, no lawyer chosen by the client) and details-only services
/// (trademark registration — no variant, no fixed price, free-text only). ServiceVariantId is
/// null for the latter.</summary>
public class CatalogRequest : ServiceRequest
{
    public int? ServiceVariantId { get; set; }
    public ServiceVariant? ServiceVariant { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? UnitPriceSnapshot { get; set; }
}
