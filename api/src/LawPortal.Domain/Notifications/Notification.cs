using LawPortal.Domain.Common;

namespace LawPortal.Domain.Notifications;

public enum NotificationType
{
    NewMessage = 1,
}

/// <summary>
/// In-app only this pass — created whenever <c>SendMessageCommand</c> finds the recipient not
/// currently connected (via <see cref="Application.Common.Interfaces.IPresenceTracker"/>), and
/// fanned out through <see cref="Application.Common.Interfaces.INotificationSender"/>, whose
/// only implementation right now just logs — no push/SMS/email vendor is contracted yet, the
/// same gap as P1's OTP sender and reCAPTCHA verifier.
/// </summary>
public class Notification : Entity<Guid>
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public Guid? RelatedThreadId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
