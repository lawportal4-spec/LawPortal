namespace LawPortal.Application.Admin.Catalog.Dtos;

public record AdminServiceVariantDto(
    int Id, int ServiceId, string NameAr, string NameEn, decimal BasePrice,
    bool RequiresQuantity, int IncludedQuantity, decimal ExtraUnitPrice,
    string? QuantityLabelAr, string? QuantityLabelEn, int MinQuantity, int MaxQuantity,
    int SortOrder, bool IsActive);

public record AdminServiceCatalogItemDto(
    int Id, int CategoryId, string NameAr, string NameEn, string Slug,
    string? DescriptionAr, string? DescriptionEn, string PricingModel, bool RequiresSpecialty,
    int SortOrder, bool IsActive, IReadOnlyList<AdminServiceVariantDto> Variants);

public record AdminServiceCategoryDto(
    int Id, string NameAr, string NameEn, string Slug, string? IconKey,
    int SortOrder, bool IsActive, IReadOnlyList<AdminServiceCatalogItemDto> Services);
