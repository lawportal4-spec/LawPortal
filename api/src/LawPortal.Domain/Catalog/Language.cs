using LawPortal.Domain.Common;

namespace LawPortal.Domain.Catalog;

public class Language : Entity<int>
{
    /// <summary>ISO 639-1 code, e.g. "ar", "en".</summary>
    public required string Code { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public bool IsActive { get; set; } = true;
}
