import axios from "axios";

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "http://localhost:5280",
});

export interface SubSpecialtyDto {
  id: number;
  nameAr: string;
  nameEn: string;
}

export interface SpecialtyDto {
  id: number;
  nameAr: string;
  nameEn: string;
  slug: string;
  subSpecialties: SubSpecialtyDto[];
}

export type ServicePricingModel = "PerLawyerFixed" | "PredefinedCatalog" | "DetailsOnly" | "CompetitiveBidding";

export interface ServiceCatalogItemDto {
  id: number;
  nameAr: string;
  nameEn: string;
  slug: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  pricingModel: ServicePricingModel;
  requiresSpecialty: boolean;
}

export interface ServiceCategoryDto {
  id: number;
  nameAr: string;
  nameEn: string;
  slug: string;
  iconKey: string | null;
  services: ServiceCatalogItemDto[];
}

export interface LawyerCardDto {
  id: string;
  slug: string;
  fullName: string;
  isVerified: boolean;
  cityNameAr: string | null;
  cityNameEn: string | null;
  avgRating: number | null;
  ratingCount: number;
  completedRequestCount: number;
  experienceDisplay: string | null;
  licenseNumber: string;
  writtenPrice: number;
  isVatRegistered: boolean;
  specialtyNamesAr: string[];
  specialtyNamesEn: string[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export type LawyerSortOption = "Newest" | "MostRequested" | "Rating" | "City" | "Experience" | "Price";

export interface LawyerSearchParams {
  specialtyId?: number;
  cityId?: number;
  regionId?: number;
  languageId?: number;
  gender?: "Male" | "Female";
  q?: string;
  sort?: LawyerSortOption;
  page?: number;
  pageSize?: number;
}

export interface LawyerLicenseDto {
  licenseNumber: string;
  issueDate: string;
  expiryDate: string;
}

export interface LawyerPricingDto {
  writtenPrice: number;
  price15: number;
  price30: number;
  price45: number;
  priceIsVatInclusive: boolean;
}

export interface LawyerQualificationDto {
  kind: string;
  titleAr: string;
  titleEn: string;
  institution: string | null;
  fromYear: number | null;
  toYear: number | null;
}

export interface LawyerProfileDetailDto {
  id: string;
  slug: string;
  fullName: string;
  bioAr: string | null;
  bioEn: string | null;
  gender: string | null;
  cityNameAr: string | null;
  cityNameEn: string | null;
  regionNameAr: string | null;
  regionNameEn: string | null;
  experienceDisplay: string | null;
  isVerified: boolean;
  isVatRegistered: boolean;
  avgRating: number | null;
  ratingCount: number;
  completedRequestCount: number;
  license: LawyerLicenseDto;
  pricing: LawyerPricingDto | null;
  specialtyNamesAr: string[];
  specialtyNamesEn: string[];
  languageNamesAr: string[];
  languageNamesEn: string[];
  qualifications: LawyerQualificationDto[];
}

export async function searchLawyers(params: LawyerSearchParams): Promise<PagedResult<LawyerCardDto>> {
  const { data } = await api.get<PagedResult<LawyerCardDto>>("/api/v1/lawyers", { params });
  return data;
}

export async function getLawyerProfile(slug: string): Promise<LawyerProfileDetailDto> {
  const { data } = await api.get<LawyerProfileDetailDto>(`/api/v1/lawyers/${slug}`);
  return data;
}

export async function getServiceCategories(): Promise<ServiceCategoryDto[]> {
  const { data } = await api.get<ServiceCategoryDto[]>("/api/v1/catalog/categories");
  return data;
}

export async function getSpecialties(): Promise<SpecialtyDto[]> {
  const { data } = await api.get<SpecialtyDto[]>("/api/v1/catalog/specialties");
  return data;
}
