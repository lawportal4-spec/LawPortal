using LawPortal.Domain.Common;

namespace LawPortal.Domain.Identity;

public class LawyerLicense : Entity<Guid>
{
    public Guid LawyerProfileId { get; set; }
    public LawyerProfile? LawyerProfile { get; set; }

    public required string LicenseNumber { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly ExpiryDate { get; set; }

    public LicenseVerificationStatus VerificationStatus { get; set; } = LicenseVerificationStatus.PendingReview;
    public Guid? VerifiedByAdminUserId { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
}
