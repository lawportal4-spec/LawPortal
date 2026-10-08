namespace LawPortal.Application.Lawyers.Dtos;

public record LawyerRequestSummaryDto(
    Guid Id,
    string Number,
    string ClientName,
    string ServiceNameAr,
    string ServiceNameEn,
    string Status,
    string? Title,
    decimal? Subtotal,
    DateTime CreatedAtUtc);

public record LawyerRequestDetailDto(
    Guid Id,
    string Number,
    string ClientName,
    string ServiceNameAr,
    string ServiceNameEn,
    string Status,
    string? SpecialtyNameAr,
    string? SpecialtyNameEn,
    string? ConsultationType,
    DateTime? ScheduledStartUtc,
    int? SelectedDurationMinutes,
    string? Title,
    string? Description,
    decimal? Subtotal,
    string CurrencyCode,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc);

public record LawyerDashboardDto(
    int AwaitingAcceptance,
    int InProgress,
    int CompletedThisMonth,
    int UnreadMessageThreads,
    decimal EarningsHeld,
    decimal EarningsReleasedThisMonth,
    decimal AvgRating,
    int RatingCount);

/// <param name="DebtOffset">Deducted at release to settle a debt to the platform; paid = Amount − DebtOffset.</param>
public record PayoutSummaryDto(Guid Id, string RequestNumber, decimal Amount, string Status, DateTime CreatedAtUtc, DateTime? ReleasedAtUtc, decimal DebtOffset);

/// <summary>What the lawyer owes the platform (refunds after they were paid) and its history.</summary>
public record MyDebtDto(decimal Balance, IReadOnlyList<MyDebtEntryDto> Entries);
public record MyDebtEntryDto(string Kind, decimal Amount, string? RequestNumber, DateTime CreatedAtUtc);

public record LawyerReviewDto(Guid Id, string RequestNumber, int Rating, string? Comment, DateTime CreatedAtUtc);

public record LawyerProfileEditDto(
    string? BioAr,
    string? BioEn,
    bool AcceptingNewRequests,
    IReadOnlyList<int> SpecialtyIds,
    IReadOnlyList<int> LanguageIds,
    /// <summary>null until the lawyer has saved prices once.</summary>
    LawyerPricingEditDto? Pricing);

public record LawyerPricingEditDto(decimal WrittenPrice, decimal Price15, decimal Price30, decimal Price45);
