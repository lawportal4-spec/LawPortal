using LawPortal.Domain.Catalog;

namespace LawPortal.Domain.Identity;

public class LawyerSpecialty
{
    public Guid LawyerProfileId { get; set; }
    public LawyerProfile? LawyerProfile { get; set; }

    public int SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

    /// <summary>Denormalized count of requests the lawyer has handled in this specialty —
    /// stays 0 until the request system (P3+) starts writing to it.</summary>
    public int RequestCount { get; set; }
    public bool IsPrimary { get; set; }
}
