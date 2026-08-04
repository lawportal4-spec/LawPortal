using FluentValidation;
using LawPortal.Application.Auth.Dtos;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public class RefreshTokenHandler(ILawPortalDbContext db, ITokenService tokenService, TokenIssuer tokenIssuer)
    : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashToken(request.RefreshToken);
        var existing = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null || !existing.IsActive || existing.User is null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        // Rotation: revoke the presented token before issuing a new pair. If a revoked/replaced
        // token is ever presented again, that is a reuse signal a later phase should react to
        // (e.g. revoke the whole token family) — out of scope for this pass.
        existing.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var result = await tokenIssuer.IssueAsync(existing.User, cancellationToken);
        return result;
    }
}
