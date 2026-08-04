namespace LawPortal.Application.Catalog.Dtos;

public record ServiceCategoryDto(
    int Id,
    string NameAr,
    string NameEn,
    string Slug,
    string? IconKey,
    IReadOnlyList<ServiceCatalogItemDto> Services);

public record ServiceCatalogItemDto(
    int Id,
    string NameAr,
    string NameEn,
    string Slug,
    string? DescriptionAr,
    string? DescriptionEn,
    string PricingModel,
    bool RequiresSpecialty);

public record ServiceVariantDto(
    int Id,
    string NameAr,
    string NameEn,
    decimal BasePrice,
    bool RequiresQuantity,
    int IncludedQuantity,
    decimal ExtraUnitPrice,
    string? QuantityLabelAr,
    string? QuantityLabelEn,
    int MinQuantity,
    int MaxQuantity);
