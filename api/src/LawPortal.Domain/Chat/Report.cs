using LawPortal.Domain.Common;

namespace LawPortal.Domain.Chat;

public enum ReportReason
{
    Harassment = 1,
    Spam = 2,
    InappropriateContent = 3,
    Other = 4,
}

/// <summary>App Store requirement for any user-to-user messaging feature — built now, alongside
/// the messaging it applies to, rather than bolted on at store-submission time.</summary>
public class Report : Entity<Guid>
{
    public Guid ReporterUserId { get; set; }
    public Guid ReportedUserId { get; set; }
    public Guid? ThreadId { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
