namespace LawPortal.Application.Offers.Dtos;

public record OfferRevisionDto(Guid Id, decimal Amount, string ProposedBy, string? Message, DateTime CreatedAtUtc);

public record OfferSummaryDto(
    Guid Id,
    Guid LawyerProfileId,
    string LawyerFullName,
    decimal? LawyerAvgRating,
    int LawyerRatingCount,
    int LawyerCompletedRequestCount,
    string Status,
    decimal CurrentAmount,
    DateTime ExpiresAtUtc,
    IReadOnlyList<OfferRevisionDto> Revisions);

public record BiddingFeedItemDto(
    Guid Id,
    string Number,
    string ServiceNameAr,
    string ServiceNameEn,
    string? SpecialtyNameAr,
    string? SpecialtyNameEn,
    string? Title,
    DateTime InvitedAtUtc,
    string? MyOfferStatus,
    decimal? MyLatestOfferAmount,
    DateTime CreatedAtUtc);

public record BiddingRequestDetailForLawyerDto(
    Guid Id,
    string Number,
    string ServiceNameAr,
    string ServiceNameEn,
    string? SpecialtyNameAr,
    string? SpecialtyNameEn,
    string? Title,
    string? Description,
    string Status,
    bool AttachmentsUnlocked,
    IReadOnlyList<string> AttachmentFileNames,
    string? MyOfferStatus,
    Guid? MyOfferId,
    decimal? MyLatestOfferAmount);
