using LawPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LawPortal.Infrastructure.Identity;

/// <summary>Dev-only stand-in for a real SMS vendor (Unifonic/Msegat/Taqnyat — none contracted
/// yet). Logs the code instead of sending it. MUST be replaced before any real phone number
/// is used — the plan's P0 open-questions list flags SMS vendor selection explicitly.</summary>
public class LoggingOtpSender(ILogger<LoggingOtpSender> logger) : IOtpSender
{
    public Task SendAsync(string phoneE164, string code, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "[DEV OTP — no SMS vendor configured] {Phone} -> {Code}. Replace IOtpSender before launch.",
            phoneE164, code);
        return Task.CompletedTask;
    }
}
