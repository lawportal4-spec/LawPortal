using LawPortal.Domain.Common;

namespace LawPortal.Domain.Chat;

/// <summary>
/// One row per (thread, user) — not one row per message. <see cref="LastReadAtUtc"/> is compared
/// against a message's <see cref="Message.SentAtUtc"/> to derive per-message read state, which
/// is the shape every real chat product converges on; a literal per-message-per-user
/// <c>ReadReceipt</c> row (as the plan's entity list first sketched) would multiply table rows
/// by participant count for no query this app actually needs.
/// </summary>
public class ThreadParticipant : Entity<Guid>
{
    public Guid ThreadId { get; set; }
    public MessageThread? Thread { get; set; }

    public Guid UserId { get; set; }
    public DateTime? LastReadAtUtc { get; set; }
}
