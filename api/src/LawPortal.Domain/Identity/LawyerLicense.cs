using LawPortal.Domain.Common;

namespace LawPortal.Domain.Identity;

public class LawyerLicense : Entity<Guid>
{
    public Guid LawyerProfileId { get; set; }
    public LawyerProfile? LawyerProfile { get; set; }

    public required string LicenseNumber { get; set; }
    public LawyerLicenseType LicenseType { get; set; } = LawyerLicenseType.Licensed;
    public DateOnly IssueDate { get; set; }
    public DateOnly ExpiryDate { get; set; }

    public LicenseVerificationStatus VerificationStatus { get; set; } = LicenseVerificationStatus.PendingReview;
    public Guid? VerifiedByAdminUserId { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>The licence scan uploaded at registration — null for lawyers registered before
    /// the upload existed. Served to admins only, via a short-lived signed URL.</summary>
    public string? DocumentStorageKey { get; set; }
    public string? DocumentFileName { get; set; }
    public string? DocumentContentType { get; set; }
}
