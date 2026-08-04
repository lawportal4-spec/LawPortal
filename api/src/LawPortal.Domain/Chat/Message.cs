using LawPortal.Domain.Common;

namespace LawPortal.Domain.Chat;

/// <summary>
/// Text-only this pass — voice notes are a real exit criterion for this phase ("an iOS-recorded
/// voice note plays on Android and the reverse"), but that's a cross-platform playback claim
/// with no way to genuinely verify it before a real mobile client exists to produce actual
/// iOS/Android recordings (P7). Building the storage/transcode plumbing now against synthetic
/// test files would be unverified code wearing the shape of a finished feature — deferred
/// on purpose rather than half-built.
/// </summary>
public class Message : Entity<Guid>
{
    public Guid ThreadId { get; set; }
    public MessageThread? Thread { get; set; }

    public Guid SenderUserId { get; set; }
    public required string Body { get; set; }

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
