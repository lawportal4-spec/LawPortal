using System.Net;
using System.Net.Http.Json;
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

/// <summary>
/// Brevo's transactional email API over HTTPS. Railway blocks outbound SMTP below its Pro plan, so
/// this is the production sender: set <c>Email:Brevo:ApiKey</c> and <c>Email:From</c> (a sender
/// address verified in Brevo). Free tier: 300 emails/day.
/// </summary>
public class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _client;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(IConfiguration configuration, ILogger<BrevoEmailSender> logger)
    {
        _logger = logger;
        _fromEmail = configuration["Email:From"] ?? throw new InvalidOperationException("Email:From is not configured.");
        _fromName = configuration["Email:FromName"] ?? "بوابة القانون";
        // BaseUrl is only overridden to point tests at a local stub.
        _client = new HttpClient { BaseAddress = new Uri(configuration["Email:Brevo:BaseUrl"] ?? "https://api.brevo.com/") };
        _client.DefaultRequestHeaders.Add("api-key", configuration["Email:Brevo:ApiKey"]);
        _client.DefaultRequestHeaders.Add("accept", "application/json");
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var content = JsonContent.Create(new
        {
            sender = new { name = _fromName, email = _fromEmail },
            to = new[] { new { email = to } },
            subject,
            htmlContent = htmlBody,
        });
        // Send a plain Content-Length body rather than chunked encoding.
        await content.LoadIntoBufferAsync(cancellationToken);

        HttpResponseMessage response;
        try
        {
            response = await _client.PostAsync("v3/smtp/email", content, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // Unreachable provider: report it the same way as a refusal, so callers handle one case.
            _logger.LogError(ex, "Brevo could not be reached to email {To}", to);
            throw new InvalidOperationException("The email could not be sent. Please try again later.", ex);
        }

        if (response.IsSuccessStatusCode) return;
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Brevo rejected the email to {To} ({Status}): {Detail}", to, (int)response.StatusCode, detail);
        throw new InvalidOperationException("The email could not be sent. Please try again later.");
    }
}

