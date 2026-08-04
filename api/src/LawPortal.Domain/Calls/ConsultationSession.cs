using LawPortal.Domain.Common;

namespace LawPortal.Domain.Calls;

public enum CallSessionStatus
{
    Scheduled = 1,
    Live = 2,
    Ended = 3,
}

/// <summary>
/// One per Instant/Scheduled `ConsultationRequest` (never Written — that's chat-only).
/// <see cref="AllowedDurationSeconds"/> comes from the paid duration tier
/// (<see cref="Requests.ConsultationRequest.SelectedDurationMinutes"/>) and is both the LiveKit
/// token's TTL and the server-side cutoff a webhook-driven check enforces — a client can't get
/// more call time by simply ignoring the token expiry and reconnecting.
/// </summary>
public class ConsultationSession : Entity<Guid>
{
    public Guid ServiceRequestId { get; set; }
    public required string RoomName { get; set; }
    public int AllowedDurationSeconds { get; set; }

    public CallSessionStatus Status { get; set; } = CallSessionStatus.Scheduled;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public int? ActualDurationSeconds { get; set; }

    /// <summary>Set once a real Egress recorder writes the file — self-hosting LiveKit's Egress
    /// service is a separate container plus its own storage wiring, a real infra lift beyond
    /// this pass's scope (same shape of deferral as ZATCA Phase 2 or a real Moyasar account).
    /// Always null this pass.</summary>
    public string? RecordingStorageKey { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
