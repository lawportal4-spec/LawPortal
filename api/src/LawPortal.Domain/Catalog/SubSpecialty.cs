using LawPortal.Domain.Common;

namespace LawPortal.Domain.Catalog;

public class SubSpecialty : Entity<int>
{
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public bool IsActive { get; set; } = true;

    public int SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }
}
