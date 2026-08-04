namespace LawPortal.Application.Subscriptions.Dtos;

public record SubscriptionPlanDto(
    int Id,
    string Slug,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    decimal MonthlyPrice,
    decimal? CommissionPercentageOverride,
    bool IncludesBroadcastBidding);

public record MySubscriptionDto(
    SubscriptionPlanDto EffectivePlan,
    Guid? SubscriptionId,
    string? Status,
    DateTime? CurrentPeriodStartUtc,
    DateTime? CurrentPeriodEndUtc,
    bool CancelAtPeriodEnd);

public record SubscriptionInvoiceDto(
    Guid Id,
    string Number,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    decimal SubtotalExVat,
    decimal VatAmount,
    decimal Total,
    string Status,
    DateTime DueAtUtc,
    DateTime? PaidAtUtc);
