using LawPortal.Domain.Common;

namespace LawPortal.Domain.Catalog;

public class City : Entity<int>
{
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public bool IsActive { get; set; } = true;

    public int RegionId { get; set; }
    public Region? Region { get; set; }
}
