using LawPortal.Domain.Common;

namespace LawPortal.Domain.Catalog;

public class Region : Entity<int>
{
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<City> Cities { get; set; } = [];
}
