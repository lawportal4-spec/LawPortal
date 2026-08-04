using LawPortal.Domain.Common;
using LawPortal.Domain.Identity;

namespace LawPortal.Domain.Pricing;

/// <summary>A lawyer's published consultation price matrix — one written-consultation price
/// plus three call-duration tiers, as documented on the reference profile screen. Versioning
/// (immutable history of price changes) is a later-phase refinement; this pass keeps exactly
/// one current row per lawyer.</summary>
public class LawyerPricing : Entity<Guid>
{
    public Guid LawyerProfileId { get; set; }
    public LawyerProfile? LawyerProfile { get; set; }

    public decimal WrittenPrice { get; set; }
    public decimal Price15 { get; set; }
    public decimal Price30 { get; set; }
    public decimal Price45 { get; set; }

    /// <summary>KSA consumer convention — published prices are VAT-inclusive by default.</summary>
    public bool PriceIsVatInclusive { get; set; } = true;
    public bool IsPublished { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
