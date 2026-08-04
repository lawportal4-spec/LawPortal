using LawPortal.Application.Admin.Finance.Dtos;
using LawPortal.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Finance.Queries;

public record GetAdminPaymentDetailQuery(Guid PaymentId) : IRequest<AdminPaymentDetailDto>;

public class GetAdminPaymentDetailHandler(ILawPortalDbContext db) : IRequestHandler<GetAdminPaymentDetailQuery, AdminPaymentDetailDto>
{
    public async Task<AdminPaymentDetailDto> Handle(GetAdminPaymentDetailQuery request, CancellationToken cancellationToken)
    {
        var p = await db.Payments
            .Include(x => x.Client).ThenInclude(c => c!.User)
            .Include(x => x.LawyerProfile)
            .Include(x => x.ServiceRequest)
            .Include(x => x.Refunds)
            .Include(x => x.Payout)
            .FirstOrDefaultAsync(x => x.Id == request.PaymentId, cancellationToken)
            ?? throw new KeyNotFoundException("Payment not found.");

        return new AdminPaymentDetailDto(
            p.Id, p.Number, p.Purpose.ToString(), p.Status.ToString(), p.MethodDescription,
            p.Total, p.VatAmount, p.CommissionAmount, p.NetToLawyerAmount, p.IsVatApplicable,
            p.Client?.FullName ?? p.Client?.User?.PhoneE164, p.LawyerProfile?.FullName, p.ServiceRequest?.Number,
            p.GatewayProvider, p.GatewayPaymentId, p.FailureReason, p.CreatedAtUtc, p.PaidAtUtc,
            p.Refunds.OrderByDescending(r => r.CreatedAtUtc)
                .Select(r => new RefundLineDto(r.Id, r.Amount, r.Reason, r.Status.ToString(), r.CreatedAtUtc)).ToList(),
            p.Payout is null ? null : new PayoutLineDto(p.Payout.Id, p.Payout.Amount, p.Payout.Status.ToString(), p.Payout.CreatedAtUtc, p.Payout.ReleasedAtUtc));
    }
}
