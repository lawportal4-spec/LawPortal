using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Users.Queries;

public record AdminUserDto(Guid UserId, string DisplayName, string Email, IReadOnlyList<string> Roles, DateTime CreatedAtUtc, DateTime? LastLoginAtUtc);

public record GetAdminUsersQuery : IRequest<IReadOnlyList<AdminUserDto>>;

public class GetAdminUsersHandler(ILawPortalDbContext db) : IRequestHandler<GetAdminUsersQuery, IReadOnlyList<AdminUserDto>>
{
    public async Task<IReadOnlyList<AdminUserDto>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var admins = await db.Users
            .Where(u => u.UserType == UserType.Admin)
            .Include(u => u.AdminProfile)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return admins.Select(u => new AdminUserDto(
            u.Id, u.AdminProfile?.DisplayName ?? "—", u.Email ?? "—",
            u.UserRoles.Select(ur => ur.Role!.Name).ToList(), u.CreatedAtUtc, u.LastLoginAtUtc)).ToList();
    }
}
