using LawPortal.Domain.Common;

namespace LawPortal.Domain.Identity;

public class OtpChallenge : Entity<Guid>
{
    public required string PhoneE164 { get; set; }
    /// <summary>HMAC of the code — the raw 6-digit code is never persisted.</summary>
    public required string CodeHash { get; set; }
    public OtpPurpose Purpose { get; set; } = OtpPurpose.Login;
    public int Attempts { get; set; }
    public const int MaxAttempts = 5;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? RequestIp { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsConsumed => ConsumedAtUtc is not null;
    public bool IsLocked => Attempts >= MaxAttempts;
}
