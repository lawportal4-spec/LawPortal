using LawPortal.Domain.Identity;

namespace LawPortal.Domain.Requests;

/// <summary>Per-lawyer fixed-price consultation. Documented wizard: specialty → lawyer → details.</summary>
public class ConsultationRequest : ServiceRequest
{
    public ConsultationType ConsultationType { get; set; }

    public Guid LawyerProfileId { get; set; }
    public LawyerProfile? LawyerProfile { get; set; }

    /// <summary>Optional, written-consultation only, capped at 5 minutes.</summary>
    public string? VoiceNoteStorageKey { get; set; }
    public int? VoiceNoteDurationSeconds { get; set; }

    /// <summary>Scheduled-consultation only.</summary>
    public DateTime? ScheduledStartUtc { get; set; }

    /// <summary>15/30/45 — required for Instant/Scheduled (the priced call-duration tiers);
    /// null for Written, which has no live call and is priced flat. Drives both which
    /// <see cref="Pricing.LawyerPricing"/> column is charged (<c>SubmitRequestHandler</c>) and
    /// how long the LiveKit token issued for the call is allowed to last (P8).</summary>
    public int? SelectedDurationMinutes { get; set; }
}
