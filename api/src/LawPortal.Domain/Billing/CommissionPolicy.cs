using LawPortal.Domain.Common;

namespace LawPortal.Domain.Billing;

/// <summary>
/// Configurable policy data, never a constant — a rate change is a new row, not a redeploy.
/// <see cref="ServiceCategorySlug"/> null means the global default; a category-specific active
/// row takes precedence when one exists.
/// </summary>
public class CommissionPolicy : Entity<int>
{
    public string? ServiceCategorySlug { get; set; }
    public decimal Percentage { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;
}
