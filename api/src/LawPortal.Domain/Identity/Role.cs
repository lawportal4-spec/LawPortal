using LawPortal.Domain.Common;

namespace LawPortal.Domain.Identity;

/// <summary>Seeded roles: Client, Lawyer, Admin. Permission-based checks, not hard-coded role checks.</summary>
public class Role : Entity<int>
{
    public required string Name { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
}

public class Permission : Entity<int>
{
    /// <summary>e.g. "lawyers.verify", "requests.refund".</summary>
    public required string Code { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public class RolePermission
{
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    public int PermissionId { get; set; }
    public Permission? Permission { get; set; }
}

public class UserRole
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public int RoleId { get; set; }
    public Role? Role { get; set; }
}
