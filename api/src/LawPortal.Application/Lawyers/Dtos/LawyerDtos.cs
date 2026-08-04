namespace LawPortal.Application.Lawyers.Dtos;

public record LawyerCardDto(
    Guid Id,
    string Slug,
    string FullName,
    bool IsVerified,
    string? CityNameAr,
    string? CityNameEn,
    decimal? AvgRating,
    int RatingCount,
    int CompletedRequestCount,
    string? ExperienceDisplay,
    string LicenseNumber,
    decimal WrittenPrice,
    bool IsVatRegistered,
    IReadOnlyList<string> SpecialtyNamesAr,
    IReadOnlyList<string> SpecialtyNamesEn);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public record LawyerPricingDto(decimal WrittenPrice, decimal Price15, decimal Price30, decimal Price45, bool PriceIsVatInclusive);

public record LawyerQualificationDto(string Kind, string TitleAr, string TitleEn, string? Institution, int? FromYear, int? ToYear);

public record LawyerLicenseDto(string LicenseNumber, DateOnly IssueDate, DateOnly ExpiryDate);

public record LawyerProfileDetailDto(
    Guid Id,
    string Slug,
    string FullName,
    string? BioAr,
    string? BioEn,
    string? Gender,
    string? CityNameAr,
    string? CityNameEn,
    string? RegionNameAr,
    string? RegionNameEn,
    string? ExperienceDisplay,
    bool IsVerified,
    bool IsVatRegistered,
    decimal? AvgRating,
    int RatingCount,
    int CompletedRequestCount,
    LawyerLicenseDto License,
    LawyerPricingDto? Pricing,
    IReadOnlyList<string> SpecialtyNamesAr,
    IReadOnlyList<string> SpecialtyNamesEn,
    IReadOnlyList<string> LanguageNamesAr,
    IReadOnlyList<string> LanguageNamesEn,
    IReadOnlyList<LawyerQualificationDto> Qualifications);
