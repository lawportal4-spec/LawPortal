using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Identity;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Account;

public record ExportedRequestDto(Guid Id, string Kind, string Status, string? Title, decimal? SubtotalAmount, DateTime SubmittedAtUtc);

public record ExportedPaymentDto(Guid Id, decimal Total, decimal VatAmount, string Status, DateTime CreatedAtUtc);

public record ExportedMessageDto(Guid ThreadId, Guid SenderUserId, bool SentByMe, string Body, DateTime SentAtUtc);

public record ExportedReviewDto(Guid RequestId, int Rating, string? Comment, DateTime CreatedAtUtc);

public record AccountExportDto(
    DateTime GeneratedAtUtc,
    Guid UserId,
    string UserType,
    string? PhoneE164,
    string? Email,
    DateTime AccountCreatedAtUtc,
    string? FullName,
    IReadOnlyList<ExportedRequestDto> Requests,
    IReadOnlyList<ExportedPaymentDto> Payments,
    IReadOnlyList<ExportedMessageDto> Messages,
    IReadOnlyList<ExportedReviewDto> ReviewsGiven,
    IReadOnlyList<ExportedReviewDto> ReviewsReceived);

/// <summary>The PDPL/data-portability counterpart to <see cref="DeleteAccountCommand"/> — the
/// right of erasure was built in P1; this is the right to receive a copy of one's own data,
/// closed in P13. Deliberately scoped to what is genuinely "this person's data": profile,
/// requests, payments, messages, and reviews — not internal accounting rows (ledger entries,
/// commission policy) or security artifacts (refresh tokens, OTP challenges) that describe the
/// platform's own operation rather than the data subject.</summary>
public record ExportMyDataQuery : IRequest<AccountExportDto>;

public class ExportMyDataHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<ExportMyDataQuery, AccountExportDto>
{
    public async Task<AccountExportDto> Handle(ExportMyDataQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException();

        string? fullName = null;
        Guid? clientProfileId = null;
        Guid? lawyerProfileId = null;

        if (user.UserType == UserType.Client)
        {
            var client = await db.ClientProfiles.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
            fullName = client?.FullName;
            clientProfileId = client?.Id;
        }
        else if (user.UserType == UserType.Lawyer)
        {
            var lawyer = await db.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == userId, cancellationToken);
            fullName = lawyer?.FullName;
            lawyerProfileId = lawyer?.Id;
        }

        var requests = clientProfileId is null
            ? []
            : await db.ServiceRequests
                .Where(r => r.ClientId == clientProfileId)
                .Select(r => new ExportedRequestDto(
                    r.Id,
                    r is ConsultationRequest ? "Consultation" : r is BiddingRequest ? "Bidding" : "Catalog",
                    r.Status.ToString(),
                    r.Title,
                    r.Subtotal,
                    r.SubmittedAtUtc ?? r.CreatedAtUtc))
                .ToListAsync(cancellationToken);

        var requestIds = requests.Select(r => r.Id).ToList();

        var payments = await db.Payments
            .Where(p => p.ServiceRequestId != null && requestIds.Contains(p.ServiceRequestId.Value))
            .Select(p => new ExportedPaymentDto(p.Id, p.Total, p.VatAmount, p.Status.ToString(), p.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var messages = await db.Messages
            .Where(m => m.Thread!.Participants.Any(tp => tp.UserId == userId))
            .OrderBy(m => m.SentAtUtc)
            .Select(m => new ExportedMessageDto(m.ThreadId, m.SenderUserId, m.SenderUserId == userId, m.Body, m.SentAtUtc))
            .ToListAsync(cancellationToken);

        var reviewsGiven = clientProfileId is null
            ? []
            : await db.Reviews
                .Where(r => r.ClientId == clientProfileId)
                .Select(r => new ExportedReviewDto(r.ServiceRequestId, r.Rating, r.Comment, r.CreatedAtUtc))
                .ToListAsync(cancellationToken);

        var reviewsReceived = lawyerProfileId is null
            ? []
            : await db.Reviews
                .Where(r => r.LawyerProfileId == lawyerProfileId)
                .Select(r => new ExportedReviewDto(r.ServiceRequestId, r.Rating, r.Comment, r.CreatedAtUtc))
                .ToListAsync(cancellationToken);

        return new AccountExportDto(
            DateTime.UtcNow,
            user.Id,
            user.UserType.ToString(),
            user.PhoneE164,
            user.Email,
            user.CreatedAtUtc,
            fullName,
            requests,
            payments,
            messages,
            reviewsGiven,
            reviewsReceived);
    }
}
