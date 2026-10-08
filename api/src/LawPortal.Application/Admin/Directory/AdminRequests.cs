using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Domain.Identity;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Directory;

// «الطلبات»: every service in the system, and one request's full story.

public record AdminRequestStatsDto(int Total, int InProgress, int CompletedThisMonth, int WithReports);
public record AdminRequestListDto(AdminRequestStatsDto Stats, PagedResult<AdminRequestRowDto> Page);

/// <param name="Type">Instant, Scheduled, Written, Bidding or Catalog.</param>
public record GetAdminRequestsQuery(
    string? Search = null, string? Type = null, RequestStatus? Status = null, DateTime? From = null, DateTime? To = null,
    Guid? ClientProfileId = null, Guid? LawyerProfileId = null, int Page = 1, int PageSize = 20) : IRequest<AdminRequestListDto>;

public class GetAdminRequestsHandler(ILawPortalDbContext db) : IRequestHandler<GetAdminRequestsQuery, AdminRequestListDto>
{

    public async Task<AdminRequestListDto> Handle(GetAdminRequestsQuery request, CancellationToken cancellationToken)
    {
        var q = db.ServiceRequests.AsQueryable();
        if (request.ClientProfileId is { } clientId) q = q.Where(r => r.ClientId == clientId);
        if (request.LawyerProfileId is { } lawyerId)
            q = q.Where(r => (r is ConsultationRequest && ((ConsultationRequest)r).LawyerProfileId == lawyerId)
                || (r is BiddingRequest && ((BiddingRequest)r).AwardedLawyerProfileId == lawyerId)
                || db.Payments.Any(p => p.ServiceRequestId == r.Id && p.LawyerProfileId == lawyerId));
        q = request.Type switch
        {
            "Instant" => q.Where(r => r is ConsultationRequest && ((ConsultationRequest)r).ConsultationType == ConsultationType.Instant),
            "Scheduled" => q.Where(r => r is ConsultationRequest && ((ConsultationRequest)r).ConsultationType == ConsultationType.Scheduled),
            "Written" => q.Where(r => r is ConsultationRequest && ((ConsultationRequest)r).ConsultationType == ConsultationType.Written),
            "Bidding" => q.Where(r => r is BiddingRequest),
            "Catalog" => q.Where(r => r is CatalogRequest),
            _ => q,
        };
        if (request.Status is { } status) q = q.Where(r => r.Status == status);
        if (request.From is { } from) q = q.Where(r => r.CreatedAtUtc >= from);
        if (request.To is { } to) q = q.Where(r => r.CreatedAtUtc <= to);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            var lawyers = db.LawyerProfiles.Where(l => l.FullName.Contains(term)).Select(l => l.Id);
            q = q.Where(r => r.Number.Contains(term)
                || (r.Client!.FullName != null && r.Client.FullName.Contains(term))
                || (r.Client.User!.PhoneE164 != null && r.Client.User.PhoneE164.Contains(Activity.PhoneDigits(term)))
                || (r is ConsultationRequest && lawyers.Contains(((ConsultationRequest)r).LawyerProfileId))
                || db.Payments.Any(p => p.ServiceRequestId == r.Id && p.LawyerProfileId != null && lawyers.Contains(p.LawyerProfileId.Value)));
        }

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var reportedRequests = db.Reports.Where(rp => rp.ThreadId != null)
            .Join(db.MessageThreads, rp => rp.ThreadId, t => t.Id, (rp, t) => t.ServiceRequestId);
        var all = request.ClientProfileId is null && request.LawyerProfileId is null ? db.ServiceRequests : q;
        var stats = new AdminRequestStatsDto(
            await all.CountAsync(cancellationToken),
            await all.CountAsync(r => r.Status == RequestStatus.Paid || r.Status == RequestStatus.InProgress || r.Status == RequestStatus.Awarded, cancellationToken),
            await all.CountAsync(r => r.Status == RequestStatus.Completed
                && r.StatusHistory.Any(h => h.ToStatus == RequestStatus.Completed && h.OccurredAtUtc >= monthStart), cancellationToken),
            await all.CountAsync(r => reportedRequests.Contains(r.Id), cancellationToken));

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var total = await q.CountAsync(cancellationToken);
        var rows = await q.OrderByDescending(r => r.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new
            {
                r.Id, r.Number, r.Status, r.CreatedAtUtc, r.Subtotal, r.ClientId,
                ClientName = r.Client!.FullName, ClientPhone = r.Client.User!.PhoneE164,
                Kind = r is ConsultationRequest ? (int)((ConsultationRequest)r).ConsultationType : r is BiddingRequest ? 100 : 200,
                LawyerId = r is ConsultationRequest ? (Guid?)((ConsultationRequest)r).LawyerProfileId
                    : r is BiddingRequest ? ((BiddingRequest)r).AwardedLawyerProfileId
                    : db.Payments.Where(p => p.ServiceRequestId == r.Id && p.LawyerProfileId != null).Select(p => p.LawyerProfileId).FirstOrDefault(),
                Paid = db.Payments.Where(p => p.ServiceRequestId == r.Id && (p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.PartiallyRefunded || p.Status == PaymentStatus.Refunded)).Sum(p => (decimal?)p.Total),
            })
            .ToListAsync(cancellationToken);
        var lawyerIds = rows.Where(r => r.LawyerId != null).Select(r => r.LawyerId!.Value).Distinct().ToList();
        var lawyerNames = await db.LawyerProfiles.IgnoreQueryFilters().Where(l => lawyerIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.FullName, cancellationToken);

        var items = rows.Select(r => new AdminRequestRowDto(r.Id, r.Number, RequestTypes.Name(r.Kind),
            r.LawyerId is { } id ? lawyerNames.GetValueOrDefault(id) : null, r.LawyerId, r.ClientName, r.ClientPhone, r.ClientId,
            r.Paid ?? r.Subtotal, r.Status.ToString(), r.CreatedAtUtc)).ToList();
        return new AdminRequestListDto(stats, new PagedResult<AdminRequestRowDto>(items, page, pageSize, total));
    }
}

internal static class RequestTypes
{
    public static string Name(int kind) => kind switch
    {
        (int)ConsultationType.Instant => "Instant",
        (int)ConsultationType.Scheduled => "Scheduled",
        (int)ConsultationType.Written => "Written",
        100 => "Bidding",
        _ => "Catalog",
    };
}

public record RequestPaymentDto(Guid Id, string Number, decimal Gross, decimal Discount, decimal Total, decimal Refunded, string Status, string Method,
    DateTime CreatedAtUtc, decimal? LawyerShare, decimal? LawyerPaidOut, string? PayoutStatus);
public record RequestHistoryDto(string? From, string To, string? Trigger, DateTime OccurredAtUtc);
public record RequestAttachmentDto(Guid Id, string FileName, string ScanStatus, string? Url);
public record RequestReviewDto(int Rating, string? Comment, DateTime CreatedAtUtc);
public record RequestReportDto(string Reason, string? Details, DateTime CreatedAtUtc);

public record AdminRequestDetailDto(
    AdminRequestRowDto Summary, string? Title, string? Description, string? CategoryNameAr, string? CategoryNameEn,
    string? ServiceNameAr, string? ServiceNameEn, DateTime? ScheduledStartUtc, string? CancelReason, int OffersCount,
    IReadOnlyList<RequestPaymentDto> Payments, IReadOnlyList<RequestHistoryDto> History, IReadOnlyList<RequestAttachmentDto> Attachments,
    RequestReviewDto? Review, IReadOnlyList<RequestReportDto> Reports, int MessagesCount, IReadOnlyList<ActivityDto> Activity);

public record GetAdminRequestQuery(Guid RequestId) : IRequest<AdminRequestDetailDto>;

public class GetAdminRequestHandler(ILawPortalDbContext db, IFileStorage storage, IMediator mediator) : IRequestHandler<GetAdminRequestQuery, AdminRequestDetailDto>
{
    public async Task<AdminRequestDetailDto> Handle(GetAdminRequestQuery request, CancellationToken cancellationToken)
    {
        var r = await db.ServiceRequests
            .Include(x => x.Service).ThenInclude(s => s!.Category)
            .Include(x => x.Attachments)
            .Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.Id == request.RequestId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        var summary = (await mediator.Send(new GetAdminRequestsQuery(Search: r.Number, PageSize: 5), cancellationToken)).Page.Items
            .First(x => x.Id == r.Id);
        var payments = await db.Payments.Where(p => p.ServiceRequestId == r.Id).OrderBy(p => p.CreatedAtUtc)
            .Select(p => new RequestPaymentDto(p.Id, p.Number, p.GrossAmount, p.DiscountAmount, p.Total,
                p.Refunds.Where(x => x.Status == RefundStatus.Completed).Sum(x => (decimal?)x.Amount) ?? 0, p.Status.ToString(), p.MethodDescription, p.CreatedAtUtc,
                p.Payout != null ? p.Payout.Amount : null,
                p.Payout != null && p.Payout.Status == PayoutStatus.Released ? p.Payout.Amount - p.Payout.DebtOffset : null,
                p.Payout != null ? p.Payout.Status.ToString() : null))
            .ToListAsync(cancellationToken);

        var threadIds = await db.MessageThreads.Where(t => t.ServiceRequestId == r.Id).Select(t => t.Id).ToListAsync(cancellationToken);
        var reports = await db.Reports.Where(x => x.ThreadId != null && threadIds.Contains(x.ThreadId.Value))
            .Select(x => new RequestReportDto(x.Reason.ToString(), x.Details, x.CreatedAtUtc)).ToListAsync(cancellationToken);
        var review = await db.Reviews.Where(x => x.ServiceRequestId == r.Id)
            .Select(x => new RequestReviewDto(x.Rating, x.Comment, x.CreatedAtUtc)).FirstOrDefaultAsync(cancellationToken);
        var messages = await db.Messages.CountAsync(m => threadIds.Contains(m.ThreadId), cancellationToken);
        var offers = r is BiddingRequest ? await db.Offers.CountAsync(o => o.ServiceRequestId == r.Id, cancellationToken) : 0;

        var ids = new List<string> { r.Id.ToString() };
        ids.AddRange(payments.Select(p => p.Id.ToString()));
        var paymentIds = payments.Select(p => p.Id).ToList();
        ids.AddRange((await db.Payouts.Where(o => paymentIds.Contains(o.PaymentId)).Select(o => o.Id).ToListAsync(cancellationToken)).Select(x => x.ToString()));
        var activity = await Activity.ForAsync(db, ids, null, cancellationToken);

        return new AdminRequestDetailDto(summary, r.Title, r.Description, r.Service?.Category?.NameAr, r.Service?.Category?.NameEn,
            r.Service?.NameAr, r.Service?.NameEn, (r as ConsultationRequest)?.ScheduledStartUtc, r.CancelReason, offers, payments,
            r.StatusHistory.OrderByDescending(h => h.OccurredAtUtc)
                .Select(h => new RequestHistoryDto(h.FromStatus.ToString(), h.ToStatus.ToString(), h.Trigger, h.OccurredAtUtc)).ToList(),
            r.Attachments.OrderBy(a => a.SortOrder).Select(a => new RequestAttachmentDto(a.Id, a.FileName, a.ScanStatus.ToString(),
                a.ScanStatus == Domain.Files.AttachmentScanStatus.Infected ? null : storage.CreateDownloadUrl(a.StorageKey, TimeSpan.FromMinutes(15)))).ToList(),
            review, reports, messages, activity);
    }
}

public record AdminChatMessageDto(string SenderRole, string? SenderName, string Body, DateTime SentAtUtc);

/// <summary>Reading a request's chat is for disputes and reports; every view is written to the audit log.</summary>
public record GetAdminRequestChatQuery(Guid RequestId) : IRequest<IReadOnlyList<AdminChatMessageDto>>;

public class GetAdminRequestChatHandler(ILawPortalDbContext db, IAuditLogger auditLogger) : IRequestHandler<GetAdminRequestChatQuery, IReadOnlyList<AdminChatMessageDto>>
{
    public async Task<IReadOnlyList<AdminChatMessageDto>> Handle(GetAdminRequestChatQuery request, CancellationToken cancellationToken)
    {
        var threads = await db.MessageThreads.Where(t => t.ServiceRequestId == request.RequestId)
            .Select(t => new { t.Id, t.ClientUserId, t.LawyerUserId }).ToListAsync(cancellationToken);
        var threadIds = threads.Select(t => t.Id).ToList();
        var lawyerUsers = threads.Select(t => t.LawyerUserId).ToHashSet();
        var raw = await db.Messages.Where(m => threadIds.Contains(m.ThreadId)).OrderBy(m => m.SentAtUtc)
            .Select(m => new { m.SenderUserId, m.Body, m.SentAtUtc }).ToListAsync(cancellationToken);
        var senderIds = raw.Select(m => m.SenderUserId).Distinct().ToList();
        var names = await db.Users.IgnoreQueryFilters().Where(u => senderIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.LawyerProfile != null ? u.LawyerProfile.FullName : u.ClientProfile != null ? u.ClientProfile.FullName ?? u.PhoneE164 : u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        await auditLogger.LogAsync("ChatViewedByAdmin", "ServiceRequest", request.RequestId.ToString(), cancellationToken: cancellationToken);
        return raw.Select(m => new AdminChatMessageDto(lawyerUsers.Contains(m.SenderUserId) ? "Lawyer" : "Client",
            names.GetValueOrDefault(m.SenderUserId), m.Body, m.SentAtUtc)).ToList();
    }
}
