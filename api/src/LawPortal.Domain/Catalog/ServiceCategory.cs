using LawPortal.Domain.Common;

namespace LawPortal.Domain.Catalog;

/// <summary>One of the 5 top-level service categories (legal consultations, judiciary &amp;
/// execution, notarization, business services, other).</summary>
public class ServiceCategory : Entity<int>
{
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public required string Slug { get; set; }
    public string? IconKey { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ServiceCatalogItem> Services { get; set; } = [];
}
