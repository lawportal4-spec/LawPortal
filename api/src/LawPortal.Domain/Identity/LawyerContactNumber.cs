using LawPortal.Domain.Common;

namespace LawPortal.Domain.Identity;

public enum ContactNumberKind
{
    Office = 1,
    Secretary = 2,
    Mobile = 3,
    Other = 4,
}

/// <summary>An extra number for the platform to reach a lawyer (office line, secretary, second
/// mobile). Admin-only — never exposed to clients.</summary>
public class LawyerContactNumber : Entity<Guid>
{
    public Guid LawyerProfileId { get; set; }
    public ContactNumberKind Kind { get; set; }
    /// <summary>Who answers, e.g. the secretary's name.</summary>
    public string? ContactName { get; set; }
    public required string PhoneE164 { get; set; }
}
