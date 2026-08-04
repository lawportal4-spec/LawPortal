namespace LawPortal.Application.Admin.Finance.Dtos;

public record AdminPaymentSummaryDto(
    Guid Id,
    string Number,
    string Purpose,
    string Status,
    decimal Total,
    string? ClientName,
    string? LawyerName,
    string? RequestNumber,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc);

public record RefundLineDto(Guid Id, decimal Amount, string Reason, string Status, DateTime CreatedAtUtc);

public record PayoutLineDto(Guid Id, decimal Amount, string Status, DateTime CreatedAtUtc, DateTime? ReleasedAtUtc);

public record AdminPaymentDetailDto(
    Guid Id,
    string Number,
    string Purpose,
    string Status,
    string MethodDescription,
    decimal Total,
    decimal VatAmount,
    decimal CommissionAmount,
    decimal NetToLawyerAmount,
    bool IsVatApplicable,
    string? ClientName,
    string? LawyerName,
    string? RequestNumber,
    string GatewayProvider,
    string? GatewayPaymentId,
    string? FailureReason,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc,
    IReadOnlyList<RefundLineDto> Refunds,
    PayoutLineDto? Payout);

public record LedgerAccountBalanceDto(string Account, decimal TotalDebits, decimal TotalCredits, decimal NetBalance);

public record LedgerSummaryDto(IReadOnlyList<LedgerAccountBalanceDto> Accounts, decimal GrandTotalDebits, decimal GrandTotalCredits, bool IsBalanced);

public record LedgerEntryDto(Guid Id, string Account, bool IsDebit, decimal Amount, string ReferenceType, Guid ReferenceId, string? Description, DateTime CreatedAtUtc);

public record CommissionPolicyDto(int Id, string? ServiceCategorySlug, decimal Percentage, bool IsActive, DateTime EffectiveFromUtc);
