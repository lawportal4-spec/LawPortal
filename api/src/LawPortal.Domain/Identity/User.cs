using LawPortal.Domain.Common;

namespace LawPortal.Domain.Identity;

public class User : AggregateRoot<Guid>
{
    /// <summary>E.164, e.g. +9665XXXXXXXX. Null for lawyer/admin accounts.</summary>
    public string? PhoneE164 { get; set; }
    public bool IsPhoneVerified { get; set; }

    /// <summary>Null for client accounts (phone-first, passwordless).</summary>
    public string? Email { get; set; }
    public bool IsEmailVerified { get; set; }

    /// <summary>Null for client accounts.</summary>
    public string? PasswordHash { get; set; }

    public required UserType UserType { get; set; }
    public UserStatus Status { get; set; } = UserStatus.PendingVerification;
    public string PreferredLocale { get; set; } = "ar";
    public DateTime? LastLoginAtUtc { get; set; }

    public ClientProfile? ClientProfile { get; set; }
    public LawyerProfile? LawyerProfile { get; set; }
    public AdminProfile? AdminProfile { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public static User CreateClient(string phoneE164)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            PhoneE164 = phoneE164,
            IsPhoneVerified = true,
            UserType = UserType.Client,
            Status = UserStatus.Active,
        };
    }

    public static User CreateLawyer(string email, string phoneE164, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            // Unverified until the registration OTP is confirmed; sign-in is refused until then.
            PhoneE164 = phoneE164,
            PasswordHash = passwordHash,
            UserType = UserType.Lawyer,
            // Stays PendingVerification until the licence is approved (see LicenseVerificationStatus).
            Status = UserStatus.PendingVerification,
        };
    }

    /// <summary>Matches <c>RbacSeeder</c>'s own bootstrap-admin shape exactly — an invited admin
    /// needs no separate verification step the way a lawyer's licence does.</summary>
    public static User CreateAdmin(string email, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            IsEmailVerified = true,
            PasswordHash = passwordHash,
            UserType = UserType.Admin,
            Status = UserStatus.Active,
        };
    }
}
