using LawPortal.Domain.Identity;

namespace LawPortal.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public record AccessTokenResult(string AccessToken, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(User user, IReadOnlyList<string> roles);

    /// <returns>The raw refresh token to hand to the client — only its hash is persisted.</returns>
    string CreateRefreshTokenRaw();
    string HashToken(string raw);
}

/// <summary>Abstracts the SMS vendor (Unifonic/Msegat/etc.) — not yet contracted, so Phase 1
/// logs the code instead of sending a real SMS. Swap the DI registration when a vendor is chosen.</summary>
public interface IOtpSender
{
    Task SendAsync(string phoneE164, string code, CancellationToken cancellationToken = default);
}

/// <summary>No real reCAPTCHA site/secret key exists yet — the dev implementation always
/// passes. Must be replaced before launch (see plan's open-questions list).</summary>
public interface IRecaptchaVerifier
{
    Task<bool> VerifyAsync(string token, CancellationToken cancellationToken = default);
}

public interface ICurrentUser
{
    Guid? UserId { get; }
    UserType? UserType { get; }
    IReadOnlyList<string> Roles { get; }
}

public interface IAuditLogger
{
    Task LogAsync(string action, string? entityType = null, string? entityId = null, string? details = null, CancellationToken cancellationToken = default);
}
