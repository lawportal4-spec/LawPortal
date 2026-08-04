namespace LawPortal.Application.Admin.Subscriptions.Dtos;

public record AdminSubscriptionPlanDto(
    int Id, string Slug, string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn,
    decimal MonthlyPrice, decimal? CommissionPercentageOverride, bool IncludesBroadcastBidding,
    int SortOrder, bool IsActive, int ActiveSubscriberCount);
