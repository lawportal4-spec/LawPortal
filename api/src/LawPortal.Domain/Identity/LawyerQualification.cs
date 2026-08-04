using LawPortal.Domain.Common;

namespace LawPortal.Domain.Identity;

public enum QualificationKind
{
    Education = 1,
    Experience = 2,
    Certification = 3,
}

/// <summary>One row on the profile's "الخبرات والمؤهلات" tab.</summary>
public class LawyerQualification : Entity<Guid>
{
    public Guid LawyerProfileId { get; set; }
    public LawyerProfile? LawyerProfile { get; set; }

    public QualificationKind Kind { get; set; }
    public required string TitleAr { get; set; }
    public required string TitleEn { get; set; }
    public string? Institution { get; set; }
    public int? FromYear { get; set; }
    public int? ToYear { get; set; }
    public int SortOrder { get; set; }
}
