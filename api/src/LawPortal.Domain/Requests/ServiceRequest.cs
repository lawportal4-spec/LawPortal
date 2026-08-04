using LawPortal.Domain.Catalog;
using LawPortal.Domain.Common;
using LawPortal.Domain.Files;
using LawPortal.Domain.Identity;

namespace LawPortal.Domain.Requests;

/// <summary>
/// The shared request aggregate. Holds everything true of every request regardless of pricing
/// model; mode-specific fields live on the TPT subtypes below. Bidding requests are P9 — no
/// subtype exists for that pricing model yet, and <see cref="ServiceCatalogItem.PricingModel"/>
/// simply has no working submission path for it until then.
///
/// Only <see cref="TransitionTo"/> may change <see cref="Status"/> — it is the one place that
/// writes <see cref="RequestStatusHistory"/>, so no code path can silently move a request
/// between states without leaving a trail.
/// </summary>
public abstract class ServiceRequest : AggregateRoot<Guid>
{
    public required string Number { get; set; }

    public Guid ClientId { get; set; }
    public ClientProfile? Client { get; set; }

    public int ServiceId { get; set; }
    public ServiceCatalogItem? Service { get; set; }

    public int? SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }
    public int? SubSpecialtyId { get; set; }
    public SubSpecialty? SubSpecialty { get; set; }

    public RequestStatus Status { get; private set; } = RequestStatus.Draft;

    /// <summary>Client-authored, free text in whatever language the client used — never
    /// translated, unlike the bilingual catalog metadata.</summary>
    public string? Title { get; set; }
    public string? Description { get; set; }

    public string CurrencyCode { get; set; } = "SAR";
    /// <summary>Price snapshot at submission time — VAT/commission math is P4's concern.</summary>
    public decimal? Subtotal { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancelReason { get; set; }

    public ICollection<RequestAttachment> Attachments { get; set; } = [];
    public ICollection<RequestStatusHistory> StatusHistory { get; set; } = [];

    /// <returns>The new history row — the caller must explicitly track it (e.g.
    /// <c>db.RequestStatusHistories.Add(...)</c>). EF Core cannot reliably discover a new
    /// client-keyed entity purely by graph traversal through a collection navigation that
    /// wasn't loaded via Include; adding it only to <see cref="StatusHistory"/> here produced
    /// a spurious UPDATE (0 rows affected) instead of an INSERT.</returns>
    public RequestStatusHistory TransitionTo(RequestStatus next, string trigger)
    {
        if (!IsLegalTransition(Status, next))
            throw new InvalidOperationException($"Cannot move a request from {Status} to {next}.");

        var history = new RequestStatusHistory
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = Id,
            FromStatus = Status,
            ToStatus = next,
            Trigger = trigger,
        };
        StatusHistory.Add(history);
        Status = next;
        return history;
    }

    private static bool IsLegalTransition(RequestStatus from, RequestStatus to) => (from, to) switch
    {
        (RequestStatus.Draft, RequestStatus.Submitted) => true,
        (RequestStatus.Draft, RequestStatus.Cancelled) => true,
        (RequestStatus.Submitted, RequestStatus.Cancelled) => true,
        (RequestStatus.Submitted, RequestStatus.Paid) => true,
        (RequestStatus.Paid, RequestStatus.Refunded) => true,
        (RequestStatus.Paid, RequestStatus.InProgress) => true,
        (RequestStatus.InProgress, RequestStatus.Completed) => true,
        (RequestStatus.InProgress, RequestStatus.Refunded) => true,
        (RequestStatus.Submitted, RequestStatus.Awarded) => true,
        (RequestStatus.Awarded, RequestStatus.Paid) => true,
        (RequestStatus.Awarded, RequestStatus.Cancelled) => true,
        (RequestStatus.Paid, RequestStatus.Completed) => true,
        _ => false,
    };
}
