using System.Net;
using System.Net.Mail;
using LawPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LawPortal.Infrastructure.Identity;

/// <summary>No email provider configured: log the message so links can still be followed in
/// development and on a staging box. Replace by setting <c>Email:Smtp:*</c>.</summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("[DEV EMAIL — no SMTP configured] To {To} | {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}

/// <summary>Plain SMTP — works with MailHog locally and with any mail provider's SMTP relay.</summary>
public class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var section = configuration.GetSection("Email:Smtp");
        using var client = new SmtpClient(section["Host"], int.TryParse(section["Port"], out var port) ? port : 587)
        {
            EnableSsl = bool.TryParse(section["EnableSsl"], out var ssl) && ssl,
        };
        if (!string.IsNullOrEmpty(section["User"]))
            client.Credentials = new NetworkCredential(section["User"], section["Password"]);

        using var message = new MailMessage(section["From"] ?? "no-reply@lawportal.sa", to, subject, htmlBody) { IsBodyHtml = true };
        await client.SendMailAsync(message, cancellationToken);
    }
}
