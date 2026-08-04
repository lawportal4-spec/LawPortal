using LawPortal.Application.Common.Interfaces;
using LawPortal.Application.Requests.Commands;
using LawPortal.Application.Requests.Dtos;
using LawPortal.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Requests.Queries;

public record GetRequestDetailQuery(Guid RequestId) : IRequest<RequestDetailDto>;

public class GetRequestDetailHandler(ILawPortalDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetRequestDetailQuery, RequestDetailDto>
{
    public async Task<RequestDetailDto> Handle(GetRequestDetailQuery request, CancellationToken cancellationToken)
    {
        var clientId = await CreateConsultationDraftHandler.ResolveClientProfileIdAsync(db, currentUser, cancellationToken);

        var r = await db.ServiceRequests
            .Include(x => x.Service)
            .Include(x => x.Specialty)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == request.RequestId && x.ClientId == clientId, cancellationToken)
            ?? throw new KeyNotFoundException("Request not found.");

        string? consultationType = null;
        Guid? lawyerProfileId = null;
        string? lawyerFullName = null;
        DateTime? scheduledStartUtc = null;
        int? voiceNoteDurationSeconds = null;
        string? variantNameAr = null;
        string? variantNameEn = null;
        int? quantity = null;
        string? sendMethod = null;
        Guid? awardedLawyerProfileId = null;
        string? awardedLawyerFullName = null;
        int? offerCount = null;

        if (r is ConsultationRequest consultation)
        {
            consultationType = consultation.ConsultationType.ToString();
            lawyerProfileId = consultation.LawyerProfileId;
            scheduledStartUtc = consultation.ScheduledStartUtc;
            voiceNoteDurationSeconds = consultation.VoiceNoteDurationSeconds;
            lawyerFullName = await db.LawyerProfiles
                .Where(l => l.Id == consultation.LawyerProfileId)
                .Select(l => l.FullName)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else if (r is CatalogRequest catalogRequest)
        {
            quantity = catalogRequest.Quantity;
            if (catalogRequest.ServiceVariantId is { } variantId)
            {
                var variant = await db.ServiceVariants.FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);
                variantNameAr = variant?.NameAr;
                variantNameEn = variant?.NameEn;
            }
        }
        else if (r is BiddingRequest bidding)
        {
            sendMethod = bidding.SendMethod.ToString();
            awardedLawyerProfileId = bidding.AwardedLawyerProfileId;
            offerCount = await db.Offers.CountAsync(o => o.ServiceRequestId == bidding.Id, cancellationToken);
            if (bidding.AwardedLawyerProfileId is { } awardedId)
                awardedLawyerFullName = await db.LawyerProfiles
                    .Where(l => l.Id == awardedId).Select(l => l.FullName).FirstOrDefaultAsync(cancellationToken);
        }

        return new RequestDetailDto(
            r.Id,
            r.Number,
            r switch { ConsultationRequest => "Consultation", BiddingRequest => "Bidding", _ => "Catalog" },
            r.Service!.NameAr,
            r.Service.NameEn,
            r.Status.ToString(),
            r.Specialty?.NameAr,
            r.Specialty?.NameEn,
            r.Title,
            r.Description,
            r.Subtotal,
            r.CurrencyCode,
            r.CreatedAtUtc,
            r.SubmittedAtUtc,
            r.CancelledAtUtc,
            r.CancelReason,
            consultationType,
            lawyerProfileId,
            lawyerFullName,
            scheduledStartUtc,
            voiceNoteDurationSeconds,
            variantNameAr,
            variantNameEn,
            quantity,
            sendMethod,
            awardedLawyerProfileId,
            awardedLawyerFullName,
            offerCount,
            r.Attachments.OrderBy(a => a.SortOrder).Select(a =>
                new AttachmentDto(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.ScanStatus.ToString())).ToList());
    }
}
