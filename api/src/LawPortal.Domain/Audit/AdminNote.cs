using LawPortal.Domain.Common;

namespace LawPortal.Domain.Audit;

/// <summary>An internal note an admin leaves on a client, lawyer or request. Only admins see it.</summary>
public class AdminNote : Entity<Guid>
{
    /// <summary>Client, Lawyer or Request.</summary>
    public required string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public required string Body { get; set; }
    public Guid? AuthorUserId { get; set; }
    public string? AuthorName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
