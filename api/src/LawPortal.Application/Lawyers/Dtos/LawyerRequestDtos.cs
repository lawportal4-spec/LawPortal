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

public record PayoutSummaryDto(Guid Id, string RequestNumber, decimal Amount, string Status, DateTime CreatedAtUtc, DateTime? ReleasedAtUtc);

public record LawyerReviewDto(Guid Id, string RequestNumber, int Rating, string? Comment, DateTime CreatedAtUtc);

public record LawyerProfileEditDto(
    string? BioAr,
    string? BioEn,
    bool AcceptingNewRequests,
    IReadOnlyList<int> SpecialtyIds,
    IReadOnlyList<int> LanguageIds);
