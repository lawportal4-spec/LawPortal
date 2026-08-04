using LawPortal.Domain.Common;
using LawPortal.Domain.Requests;

namespace LawPortal.Domain.Files;

public enum AttachmentScanStatus
{
    Pending = 1,
    Clean = 2,
    Infected = 3,
    ScanFailed = 4,
}

/// <summary>One of up to 5 files on a request, per the documented upload limit. Never served
/// from a public bucket — every read goes through a signed URL minted after a visibility
/// check, and a file link is dead on arrival until <see cref="ScanStatus"/> is Clean.</summary>
public class RequestAttachment : Entity<Guid>
{
    public Guid ServiceRequestId { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }

    public required string StorageKey { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }

    public AttachmentScanStatus ScanStatus { get; set; } = AttachmentScanStatus.Pending;
    public string? ScanResult { get; set; }

    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
