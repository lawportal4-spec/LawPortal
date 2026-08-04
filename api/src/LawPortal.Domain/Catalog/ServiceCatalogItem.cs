using LawPortal.Domain.Common;

namespace LawPortal.Domain.Catalog;

/// <summary>How a service's price is determined — drives which request wizard the frontend
/// renders and which fields the backend expects. <see cref="CompetitiveBidding"/> services are
/// browsable now but have no working submission flow until P9.</summary>
public enum ServicePricingModel
{
    PerLawyerFixed = 1,
    PredefinedCatalog = 2,
    DetailsOnly = 3,
    CompetitiveBidding = 4,
}

/// <summary>One of the ~20 specific services within a category (e.g. "استشارة كتابية",
/// "إنشاء وكالة").</summary>
public class ServiceCatalogItem : Entity<int>
{
    public int CategoryId { get; set; }
    public ServiceCategory? Category { get; set; }

    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public required string Slug { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ServicePricingModel PricingModel { get; set; }
    /// <summary>False for every PredefinedCatalog (notarization) service and for trademark
    /// registration — both documented as single-screen flows with no specialty step.</summary>
    public bool RequiresSpecialty { get; set; } = true;

    public ICollection<ServiceVariant> Variants { get; set; } = [];
}
