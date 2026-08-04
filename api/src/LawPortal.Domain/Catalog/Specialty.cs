using LawPortal.Domain.Common;

namespace LawPortal.Domain.Catalog;

/// <summary>
/// One of the 13 legal specialties shared across every service category. Sub-specialties are
/// populated only for some specialties, and are surfaced only on the competitive-bidding path,
/// never on the consultation path.
/// </summary>
public class Specialty : Entity<int>
{
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public required string Slug { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SubSpecialty> SubSpecialties { get; set; } = [];
}
