namespace LawPortal.Application.Common.Interfaces;

/// <summary>No push/SMS/email vendor is contracted yet — same gap as P1's <c>IOtpSender</c> and
/// <c>IRecaptchaVerifier</c>. The dev implementation logs instead of delivering anything.</summary>
public interface INotificationSender
{
    Task SendAsync(Guid userId, string title, string body, CancellationToken cancellationToken = default);
}
