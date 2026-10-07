using LawPortal.Domain.Catalog;
using LawPortal.Domain.Common;

namespace LawPortal.Domain.Identity;

public class ClientProfile : Entity<Guid>
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string? FullName { get; set; }
    public AccountKind AccountKind { get; set; } = AccountKind.Individual;
    public string? CompanyName { get; set; }
    public string? CrNumber { get; set; }
    public int? CityId { get; set; }
    public City? City { get; set; }
    public DateTime? ProfileCompletedAtUtc { get; set; }

    /// <summary>Bump when the pledge wording changes: every client whose <see cref="PledgeVersion"/> is
    /// lower is asked to accept it again on their next visit.</summary>
    public const int CurrentPledgeVersion = 1;

    /// <summary>The "أتعهد…" pledge (pay and communicate inside the platform only). 0 = never accepted.</summary>
    public int PledgeVersion { get; set; }
    public DateTime? PledgeAcceptedAtUtc { get; set; }

    public bool HasAcceptedCurrentPledge => PledgeVersion >= CurrentPledgeVersion;
}
