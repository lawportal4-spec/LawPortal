using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Rbac.Queries;

public record RoleDto(int Id, string Name, IReadOnlyList<string> Permissions);

/// <summary>Read-only — every admin action in this app is still authorized at the role level
/// (<c>[Authorize(Roles = "Admin")]</c>), not per-permission, so this is a transparency view of
/// the role→permission mapping <c>RbacSeeder</c> maintains, not a live enforcement switchboard.
/// See the P11 progress log for why per-permission enforcement isn't built yet.</summary>
public record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>;

public class GetRolesHandler(ILawPortalDbContext db) : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        return roles.Select(r => new RoleDto(r.Id, r.Name, r.RolePermissions.Select(rp => rp.Permission!.Code).OrderBy(c => c).ToList())).ToList();
    }
}
