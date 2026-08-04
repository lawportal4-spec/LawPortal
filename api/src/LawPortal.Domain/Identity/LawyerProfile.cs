using LawPortal.Domain.Catalog;
using LawPortal.Domain.Common;
using LawPortal.Domain.Pricing;

namespace LawPortal.Domain.Identity;

public enum Gender
{
    Male = 1,
    Female = 2,
}

public class LawyerProfile : AggregateRoot<Guid>
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public required string FullName { get; set; }
    public required string Slug { get; set; }
    public string? BioAr { get; set; }
    public string? BioEn { get; set; }
    public Gender? Gender { get; set; }

    public int? CityId { get; set; }
    public City? City { get; set; }
    public int? RegionId { get; set; }
    public Region? Region { get; set; }
    public int? ExperienceRangeId { get; set; }
    public ExperienceRange? ExperienceRange { get; set; }

    public bool IsVerified { get; set; }
    public bool IsVatRegistered { get; set; }
    public string? VatNumber { get; set; }

    /// <summary>A simple on/off toggle, not a calendar — a full availability schedule belongs
    /// with P8's real session scheduling, once there are actual bookable time slots to manage.</summary>
    public bool AcceptingNewRequests { get; set; } = true;

    /// <summary>Denormalized for directory sort/display — recomputed once reviews (later
    /// phase) and completed orders (P3+) exist; seeded synthetically for demo data until then.</summary>
    public decimal? AvgRating { get; set; }
    public int RatingCount { get; set; }
    public int CompletedRequestCount { get; set; }

    public LawyerLicense? License { get; set; }
    public LawyerPricing? Pricing { get; set; }
    public ICollection<LawyerSpecialty> LawyerSpecialties { get; set; } = [];
    public ICollection<LawyerLanguage> LawyerLanguages { get; set; } = [];
    public ICollection<LawyerQualification> Qualifications { get; set; } = [];
}
