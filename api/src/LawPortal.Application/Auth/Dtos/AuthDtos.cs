namespace LawPortal.Application.Auth.Dtos;

public record AuthResultDto(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    Guid UserId,
    string UserType);

public record MeDto(
    Guid UserId,
    string UserType,
    string? Phone,
    string? Email,
    string PreferredLocale,
    bool ProfileCompleted,
    IReadOnlyList<string> Roles,
    string? LawyerLicenseStatus,
    /// <summary>Clients only: false until the current platform pledge is accepted. Always true for
    /// other account types.</summary>
    bool PledgeAccepted);
