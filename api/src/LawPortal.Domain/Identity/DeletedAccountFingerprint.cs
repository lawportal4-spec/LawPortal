using LawPortal.Domain.Common;

namespace LawPortal.Domain.Identity;

/// <summary>Left behind when a lawyer deletes their account, so a later sign-up by the same person
/// can be recognised (and any debt carried over) without keeping their personal data: only one-way
/// hashes. The exception is <see cref="ContactEmail"/>/<see cref="ContactPhone"/>, kept only while
/// they owe the platform money, for collecting it — the deletion screen says so.</summary>
public class DeletedAccountFingerprint : Entity<Guid>
{
    public Guid FormerUserId { get; set; }
    public Guid LawyerProfileId { get; set; }
    public string? PhoneHash { get; set; }
    public string? EmailHash { get; set; }
    public string? NationalIdHash { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public DateTime DeletedAtUtc { get; set; } = DateTime.UtcNow;
}
