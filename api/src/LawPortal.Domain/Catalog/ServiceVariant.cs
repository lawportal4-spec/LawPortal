using LawPortal.Domain.Common;

namespace LawPortal.Domain.Catalog;

/// <summary>A priced option under a <see cref="ServicePricingModel.PredefinedCatalog"/> service
/// — e.g. "وكالة شركات" (750 SAR) vs. "وكالة فردية" (400 SAR) under "إنشاء وكالة". Party-count
/// pricing (the "عدد أطراف الوكالة" stepper) is one formula:
/// price = BasePrice + max(0, Qty - IncludedQuantity) * ExtraUnitPrice — flat pricing is just
/// ExtraUnitPrice = 0.</summary>
public class ServiceVariant : Entity<int>
{
    public int ServiceId { get; set; }
    public ServiceCatalogItem? Service { get; set; }

    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public decimal BasePrice { get; set; }

    public bool RequiresQuantity { get; set; }
    public int IncludedQuantity { get; set; } = 1;
    public decimal ExtraUnitPrice { get; set; }
    public string? QuantityLabelAr { get; set; }
    public string? QuantityLabelEn { get; set; }
    public int MinQuantity { get; set; } = 1;
    public int MaxQuantity { get; set; } = 20;

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public decimal PriceFor(int quantity)
    {
        var extraUnits = Math.Max(0, quantity - IncludedQuantity);
        return BasePrice + extraUnits * ExtraUnitPrice;
    }
}
