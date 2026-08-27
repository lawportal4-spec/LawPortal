using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Lawyers.Commands;
using LawPortal.Application.Lawyers.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Lawyers.Queries;

public record GetIncomingRequestDetailQuery(Guid RequestId) : IRequest<LawyerRequestDetailDto>;

public class GetIncomingRequestDetailHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetIncomingRequestDetailQuery, LawyerRequestDetailDto>
{
    public async Task<LawyerRequestDetailDto> Handle(GetIncomingRequestDetailQuery request, CancellationToken cancellationToken)
    {
        var lawyerProfileId = await LawyerRequestGuard.ResolveLawyerProfileIdAsync(db, currentUser, cancellationToken);

        var c = await db.ConsultationRequests
            .Include(x => x.Service)
            .Include(x => x.Specialty)
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Id == request.RequestId && x.LawyerProfileId == lawyerProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        return new LawyerRequestDetailDto(
            c.Id, c.Number, c.Client?.FullName ?? "—", c.Service!.NameAr, c.Service.NameEn, c.Status.ToString(),
            c.Specialty?.NameAr, c.Specialty?.NameEn, c.ConsultationType.ToString(), c.ScheduledStartUtc, c.SelectedDurationMinutes,
            c.Title, c.Description, c.Subtotal, c.CurrencyCode, c.CreatedAtUtc, c.SubmittedAtUtc);
    }
}
