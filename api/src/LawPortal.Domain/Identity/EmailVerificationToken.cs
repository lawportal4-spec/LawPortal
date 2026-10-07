using LawPortal.Domain.Common;

namespace LawPortal.Domain.Identity;

/// <summary>The "تفعيل الآن" link in the verification email. Only the hash is stored, like
/// <see cref="RefreshToken"/>; the raw value lives in the emailed link alone.</summary>
public class EmailVerificationToken : Entity<Guid>
{
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }

    /// <summary>Set when the link confirms a change of address: following it replaces the
    /// account's email with this one. Null for the post-approval verification of the current email.</summary>
    public string? NewEmail { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
