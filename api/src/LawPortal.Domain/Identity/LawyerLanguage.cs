using LawPortal.Domain.Catalog;

namespace LawPortal.Domain.Identity;

public class LawyerLanguage
{
    public Guid LawyerProfileId { get; set; }
    public LawyerProfile? LawyerProfile { get; set; }

    public int LanguageId { get; set; }
    public Language? Language { get; set; }
}
