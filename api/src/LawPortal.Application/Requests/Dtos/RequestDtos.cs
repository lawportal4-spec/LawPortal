namespace LawPortal.Application.Requests.Dtos;

public record AttachmentDto(Guid Id, string FileName, string ContentType, long SizeBytes, string ScanStatus);

public record RequestSummaryDto(
    Guid Id,
    string Number,
    string Kind,
    string ServiceNameAr,
    string ServiceNameEn,
    string Status,
    string? Title,
    decimal? Subtotal,
    DateTime CreatedAtUtc);

public record RequestDetailDto(
    Guid Id,
    string Number,
    string Kind,
    string ServiceNameAr,
    string ServiceNameEn,
    string Status,
    string? SpecialtyNameAr,
    string? SpecialtyNameEn,
    string? Title,
    string? Description,
    decimal? Subtotal,
    string CurrencyCode,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? CancelledAtUtc,
    string? CancelReason,
    // Consultation-only
    string? ConsultationType,
    Guid? LawyerProfileId,
    string? LawyerFullName,
    DateTime? ScheduledStartUtc,
    int? VoiceNoteDurationSeconds,
    // Catalog-only
    string? VariantNameAr,
    string? VariantNameEn,
    int? Quantity,
    // Bidding-only
    string? SendMethod,
    Guid? AwardedLawyerProfileId,
    string? AwardedLawyerFullName,
    int? OfferCount,
    IReadOnlyList<AttachmentDto> Attachments);
