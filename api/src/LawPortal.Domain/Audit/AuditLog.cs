using LawPortal.Domain.Common;

namespace LawPortal.Domain.Audit;

public class AuditLog : Entity<Guid>
{
    public Guid? ActorUserId { get; set; }
    public string? ActorRole { get; set; }
    /// <summary>e.g. "LawyerLicenseApproved", "AccountDeleted".</summary>
    public required string Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? Ip { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
