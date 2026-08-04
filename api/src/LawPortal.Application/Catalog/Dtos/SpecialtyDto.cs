namespace LawPortal.Application.Catalog.Dtos;

public record SpecialtyDto(
    int Id,
    string NameAr,
    string NameEn,
    string Slug,
    IReadOnlyList<SubSpecialtyDto> SubSpecialties);

public record SubSpecialtyDto(
    int Id,
    string NameAr,
    string NameEn);
