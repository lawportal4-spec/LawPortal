using LawPortal.Domain.Common;

namespace LawPortal.Domain.Chat;

/// <summary>One-directional — A blocking B doesn't imply B blocked A. <c>SendMessageCommand</c>
/// checks both directions before allowing a message through.</summary>
public class Block : Entity<Guid>
{
    public Guid BlockerUserId { get; set; }
    public Guid BlockedUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
