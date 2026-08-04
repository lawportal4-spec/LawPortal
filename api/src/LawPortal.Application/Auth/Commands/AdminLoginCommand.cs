using FluentValidation;
using LawPortal.Application.Auth.Dtos;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Auth.Commands;

/// <summary>Email + password only for this pass — TOTP two-factor is a later-phase addition
/// (see plan's Admin auth notes), not something to block the verification queue on.</summary>
public record AdminLoginCommand(string Email, string Password) : IRequest<AuthResultDto>;

public class AdminLoginValidator : AbstractValidator<AdminLoginCommand>
{
    public AdminLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class AdminLoginHandler(ILawPortalDbContext db, IPasswordHasher passwordHasher, TokenIssuer tokenIssuer, IAuditLogger auditLogger)
    : IRequestHandler<AdminLoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Email == request.Email && u.UserType == UserType.Admin, cancellationToken);

        if (user is null || user.PasswordHash is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Incorrect email or password.");

        var result = await tokenIssuer.IssueAsync(user, cancellationToken);
        await auditLogger.LogAsync("AdminLoggedIn", nameof(User), user.Id.ToString(), cancellationToken: cancellationToken);
        return result;
    }
}
