using LawPortal.Domain.Common;

namespace LawPortal.Domain.Identity;

public class AdminProfile : Entity<Guid>
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public required string DisplayName { get; set; }
}
