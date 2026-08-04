namespace LawPortal.Domain.Identity;

/// <summary>Canonical role name constants — the single source of truth referenced by both the
/// Application layer (handlers) and the Infrastructure seeder, so Application never needs to
/// depend on Infrastructure.</summary>
public static class RoleNames
{
    public const string Client = "Client";
    public const string Lawyer = "Lawyer";
    public const string Admin = "Admin";
}
