using LawPortal.Application.Auth.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Auth;

/// <summary>Shared "issue access + refresh token, persist the refresh token hash" step used by
/// every login/registration path.</summary>
public class TokenIssuer(ILawPortalDbContext db, ITokenService tokenService)
{
    public async Task<AuthResultDto> IssueAsync(User user, CancellationToken cancellationToken)
    {
        var roles = await GetRoleNamesAsync(user.Id, cancellationToken);
        var access = tokenService.CreateAccessToken(user, roles);
        var refreshRaw = tokenService.CreateRefreshTokenRaw();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenService.HashToken(refreshRaw),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            CreatedAtUtc = DateTime.UtcNow,
        });
        user.LastLoginAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(access.AccessToken, access.ExpiresAtUtc, refreshRaw, user.Id, user.UserType.ToString());
    }

    private async Task<List<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role!.Name)
            .ToListAsync(cancellationToken);
    }
}
