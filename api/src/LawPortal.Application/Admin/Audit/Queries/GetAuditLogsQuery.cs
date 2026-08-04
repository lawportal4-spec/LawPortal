using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Audit.Queries;

public record AuditLogDto(
    Guid Id, Guid? ActorUserId, string? ActorRole, string Action,
    string? EntityType, string? EntityId, string? Details, string? Ip, DateTime OccurredAtUtc);

/// <summary>A read-only browser over the <c>AuditLog</c> table every write path in this app has
/// been appending to since P1 (account deletion, lawyer verification/rejection, logins) — this
/// query adds no new instrumentation, it's the first thing to actually read what's already
/// there.</summary>
public record GetAuditLogsQuery(string? Action, DateTime? From, DateTime? To, int Page, int PageSize) : IRequest<PagedResult<AuditLogDto>>;

public class GetAuditLogsHandler(ILawPortalDbContext db) : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Action)) query = query.Where(a => a.Action == request.Action);
        if (request.From is { } from) query = query.Where(a => a.OccurredAtUtc >= from);
        if (request.To is { } to) query = query.Where(a => a.OccurredAtUtc <= to);
        query = query.OrderByDescending(a => a.OccurredAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(a.Id, a.ActorUserId, a.ActorRole, a.Action, a.EntityType, a.EntityId, a.Details, a.Ip, a.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, page, pageSize, totalCount);
    }
}
