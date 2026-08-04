using LawPortal.Domain.Common;

namespace LawPortal.Domain.Identity;

public class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>SHA-256 of the raw token — the raw value is never persisted.</summary>
    public required string TokenHash { get; set; }
    public string? DeviceId { get; set; }
    public string? Platform { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
}
