using LawPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LawPortal.Infrastructure.Notifications;

/// <summary>Dev-only stand-in — no push/SMS/email vendor is contracted yet, same gap as
/// <see cref="Identity.LoggingOtpSender"/>. Logs instead of delivering anything.</summary>
public class LoggingNotificationSender(ILogger<LoggingNotificationSender> logger) : INotificationSender
{
    public Task SendAsync(Guid userId, string title, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[DEV NOTIFICATION — no push/SMS/email vendor configured] -> {UserId}: {Title} — {Body}",
            userId, title, body);
        return Task.CompletedTask;
    }
}
