using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Users.Commands;

/// <summary>Only one Admin role has ever existed in this system (see <c>RbacSeeder</c>) — every
/// invited admin gets it. A genuine multi-role RBAC editor (creating new roles, reassigning
/// permissions) is a real feature with no real use case yet and isn't built here; see the P11
/// progress log's "not done" note.</summary>
public record InviteAdminCommand(string DisplayName, string Email, string Password) : IRequest<Guid>;

public class InviteAdminValidator : AbstractValidator<InviteAdminCommand>
{
    public InviteAdminValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain a digit.");
    }
}

public class InviteAdminHandler(ILawPortalDbContext db, IPasswordHasher passwordHasher, IAuditLogger auditLogger)
    : IRequestHandler<InviteAdminCommand, Guid>
{
    public async Task<Guid> Handle(InviteAdminCommand request, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            throw new InvalidOperationException("An account with this email already exists.");

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Admin, cancellationToken)
            ?? throw new InvalidOperationException("The Admin role is not seeded.");

        var user = User.CreateAdmin(request.Email, passwordHasher.Hash(request.Password));
        user.AdminProfile = new AdminProfile { Id = Guid.NewGuid(), UserId = user.Id, DisplayName = request.DisplayName };
        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("AdminInvited", nameof(User), user.Id.ToString(), $"email={request.Email}", cancellationToken);

        return user.Id;
    }
}
