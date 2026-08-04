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
}
