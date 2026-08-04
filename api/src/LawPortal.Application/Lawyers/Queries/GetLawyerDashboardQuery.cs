using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Lawyers.Dtos;
using LawPortal.Domain.Payments;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Queries;

public record GetLawyerDashboardQuery : IRequest<LawyerDashboardDto>;

public class GetLawyerDashboardHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetLawyerDashboardQuery, LawyerDashboardDto>
{
    public async Task<LawyerDashboardDto> Handle(GetLawyerDashboardQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);
        var lawyer = await db.LawyerProfiles.FirstAsync(l => l.Id == lawyerProfileId, cancellationToken);
        var userId = lawyer.UserId;
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var awaitingAcceptance = await db.ConsultationRequests
            .CountAsync(c => c.LawyerProfileId == lawyerProfileId && c.Status == RequestStatus.Paid, cancellationToken);
        var inProgress = await db.ConsultationRequests
            .CountAsync(c => c.LawyerProfileId == lawyerProfileId && c.Status == RequestStatus.InProgress, cancellationToken);
        var completedThisMonth = await db.ConsultationRequests
            .CountAsync(c => c.LawyerProfileId == lawyerProfileId && c.Status == RequestStatus.Completed && c.UpdatedAtUtc >= monthStart, cancellationToken);

        var earningsHeld = await db.Payouts
            .Where(o => o.LawyerProfileId == lawyerProfileId && o.Status == PayoutStatus.Held)
            .SumAsync(o => (decimal?)o.Amount, cancellationToken) ?? 0m;
        var earningsReleasedThisMonth = await db.Payouts
            .Where(o => o.LawyerProfileId == lawyerProfileId && o.Status == PayoutStatus.Released && o.ReleasedAtUtc >= monthStart)
            .SumAsync(o => (decimal?)o.Amount, cancellationToken) ?? 0m;

        var myThreads = await db.MessageThreads
            .Where(t => t.LawyerUserId == userId)
            .Select(t => new
            {
                LastRead = t.Participants.Where(p => p.UserId == userId).Select(p => p.LastReadAtUtc).FirstOrDefault(),
                LastMessageAt = t.Messages.OrderByDescending(m => m.SentAtUtc).Select(m => (DateTime?)m.SentAtUtc).FirstOrDefault(),
                HasUnread = t.Messages.Any(m => m.SenderUserId != userId
                    && (t.Participants.Where(p => p.UserId == userId).Select(p => p.LastReadAtUtc).FirstOrDefault() == null
                        || m.SentAtUtc > t.Participants.Where(p => p.UserId == userId).Select(p => p.LastReadAtUtc).FirstOrDefault()!.Value)),
            })
            .ToListAsync(cancellationToken);
        var unreadThreads = myThreads.Count(t => t.HasUnread);

        return new LawyerDashboardDto(
            awaitingAcceptance, inProgress, completedThisMonth, unreadThreads,
            earningsHeld, earningsReleasedThisMonth, lawyer.AvgRating ?? 0m, lawyer.RatingCount);
    }
}
