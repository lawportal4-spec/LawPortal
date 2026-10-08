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

/// <param name="LawyerPortion">What this refund took out of the lawyer's share (from its ledger posting);
/// likewise VAT and commission. <c>DiscountSupportPortion</c> is the slice of platform-funded discount it undid.</param>
public record RefundLineDto(Guid Id, decimal Amount, string Reason, string? Details, string Status, DateTime CreatedAtUtc, string? GatewayRefundId,
    decimal LawyerPortion, decimal VatPortion, decimal CommissionPortion, decimal DiscountSupportPortion, string? LawyerShareBearer);

public record PaymentDiscountDto(Guid Id, string Code, string Kind, decimal Value, decimal? MaxDiscountAmount, IReadOnlyList<string> Scopes);

public record PayoutLineDto(Guid Id, decimal Amount, string Status, DateTime CreatedAtUtc, DateTime? ReleasedAtUtc,
    decimal DebtOffset, Guid LawyerProfileId, decimal LawyerDebtBalance);

/// <summary>Only once the lawyer has been paid: refunds are allowed until <c>DeadlineUtc</c>.</summary>
public record RefundWindowDto(int WindowDays, DateTime DeadlineUtc, bool Expired);

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
    PayoutLineDto? Payout,
    decimal GrossAmount,
    decimal DiscountAmount,
    GatewayTransactionDto? Transaction,
    /// <summary>For a wallet payment: the wallet transaction that paid it.</summary>
    Guid? WalletTransactionId,
    PaymentDiscountDto? Discount,
    /// <summary>What the platform paid towards the discount (excl. VAT): the shares are worked out on the
    /// full price, so they exceed what the client paid by this much.</summary>
    decimal DiscountSupport,
    /// <summary>Consultation / bidding / catalog request type, for the page subtitle.</summary>
    string? RequestType,
    RefundWindowDto? RefundWindow,
    Guid? LawyerProfileId,
    /// <summary>The client's phone, used to find their other payments (the name may be empty).</summary>
    string? ClientPhoneE164,
    Guid ClientProfileId,
    Guid? ServiceRequestId);

public record GatewayTransactionDto(
    string? TransactionId, string? SourceType, string? CardBrand, string? CardMasked, string? ReferenceNumber,
    string? AuthorizationCode, string? ResponseCode, string? Message, decimal? Fee, DateTime FetchedAtUtc);

public record LedgerAccountBalanceDto(string Account, decimal TotalDebits, decimal TotalCredits, decimal NetBalance);

public record LedgerSummaryDto(IReadOnlyList<LedgerAccountBalanceDto> Accounts, decimal GrandTotalDebits, decimal GrandTotalCredits, bool IsBalanced);

public record LedgerEntryDto(Guid Id, string Account, bool IsDebit, decimal Amount, string ReferenceType, Guid ReferenceId, string? Description, DateTime CreatedAtUtc);

/// <param name="State">Applied (in use now), Scheduled (starts later) or Stopped.</param>
public record CommissionPolicyDto(int Id, string? ServiceCategorySlug, decimal Percentage, bool IsActive, DateTime EffectiveFromUtc, string State);

/// <summary>One posting: every ledger line written together for one money movement.</summary>
/// <param name="Kind">Checkout, TopUp, Refund, Payout, Subscription or RegistrationFee.</param>
/// <param name="Number">The human reference (PAY-…, SUB-…, REG-…).</param>
/// <param name="PaymentId">Set when the posting belongs to a payment the admin can open.</param>
public record JournalEntryDto(
    string ReferenceType, Guid ReferenceId, string Kind, string? Number, Guid? PaymentId,
    DateTime CreatedAtUtc, IReadOnlyList<JournalLineDto> Lines);

public record JournalLineDto(string Account, bool IsDebit, decimal Amount, string? Description);
