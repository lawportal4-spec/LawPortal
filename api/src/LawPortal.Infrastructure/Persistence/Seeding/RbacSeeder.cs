using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Infrastructure.Persistence.Seeding;

/// <summary>Seeds the three base roles (see Domain.Identity.RoleNames), the P1-relevant
/// permission set, and a dev-only bootstrap admin account. Idempotent.</summary>
public static class RbacSeeder
{
    /// <summary>Dev-only seed credentials — rotate/remove before any non-local deployment.</summary>
    public const string DevAdminEmail = "admin@lawportal.sa";
    public const string DevAdminPassword = "Admin123!@#";

    private static readonly string[] Permissions =
    [
        "lawyers.verify",
        "lawyers.reject",
        "lawyers.view",
        "users.view",
        "audit.view",
        // P11 — added to an already-seeded table, so these must be inserted additively (see the
        // fix below) rather than skipped by an "any rows exist" idempotency check. That exact
        // bug — a seeder that only ever checks "does anything exist" and silently stops topping
        // up once it does — already bit this project once in P6 (DevLawyerSeeder); fixed here
        // before it could repeat.
        "catalog.manage",
        "finance.manage",
        "subscriptions.manage",
        "dashboard.view",
        "admins.manage",
    ];

    public static async Task SeedAsync(LawPortalDbContext db, IPasswordHasher passwordHasher, CancellationToken cancellationToken = default)
    {
        if (!await db.Roles.AnyAsync(cancellationToken))
        {
            await db.Roles.AddRangeAsync(
            [
                new Role { Name = RoleNames.Client },
                new Role { Name = RoleNames.Lawyer },
                new Role { Name = RoleNames.Admin },
            ], cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        var existingCodes = await db.Permissions.Select(p => p.Code).ToListAsync(cancellationToken);
        var missingCodes = Permissions.Except(existingCodes).ToList();
        if (missingCodes.Count > 0)
        {
            await db.Permissions.AddRangeAsync(missingCodes.Select(code => new Permission { Code = code }), cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        var adminRole = await db.Roles.SingleAsync(r => r.Name == RoleNames.Admin, cancellationToken);
        var allPermissions = await db.Permissions.ToListAsync(cancellationToken);
        var grantedPermissionIds = await db.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);
        var ungrantedPermissions = allPermissions.Where(p => !grantedPermissionIds.Contains(p.Id)).ToList();
        if (ungrantedPermissions.Count > 0)
        {
            await db.RolePermissions.AddRangeAsync(
                ungrantedPermissions.Select(p => new RolePermission { RoleId = adminRole.Id, PermissionId = p.Id }),
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Users.AnyAsync(u => u.UserType == UserType.Admin, cancellationToken))
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = DevAdminEmail,
                IsEmailVerified = true,
                PasswordHash = passwordHasher.Hash(DevAdminPassword),
                UserType = UserType.Admin,
                Status = UserStatus.Active,
            };
            adminUser.AdminProfile = new AdminProfile { Id = Guid.NewGuid(), UserId = adminUser.Id, DisplayName = "Platform Admin" };
            db.Users.Add(adminUser);
            db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
