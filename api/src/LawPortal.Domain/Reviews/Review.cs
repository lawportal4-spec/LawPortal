using LawPortal.Domain.Common;

namespace LawPortal.Domain.Reviews;

/// <summary>
/// One per completed <c>ServiceRequest</c> (unique index) — P2 deferred real reviews entirely,
/// seeding only denormalized aggregate fields on <c>LawyerProfile</c> since no request system
/// existed yet to attach a review to. Now one does: the first real review for a given lawyer
/// overwrites that lawyer's synthetic seeded <c>AvgRating</c>/<c>RatingCount</c> with the real
/// computed values — see <c>SubmitReviewHandler</c>.
/// </summary>
public class Review : Entity<Guid>
{
    public Guid ServiceRequestId { get; set; }
    public Guid ClientId { get; set; }
    public Guid LawyerProfileId { get; set; }

    public int Rating { get; set; }
    public string? Comment { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
