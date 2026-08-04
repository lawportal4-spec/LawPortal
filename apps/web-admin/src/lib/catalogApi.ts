import { api } from "./api";

export interface AdminServiceVariantDto {
  id: number;
  serviceId: number;
  nameAr: string;
  nameEn: string;
  basePrice: number;
  requiresQuantity: boolean;
  includedQuantity: number;
  extraUnitPrice: number;
  quantityLabelAr: string | null;
  quantityLabelEn: string | null;
  minQuantity: number;
  maxQuantity: number;
  sortOrder: number;
  isActive: boolean;
}

export interface AdminServiceCatalogItemDto {
  id: number;
  categoryId: number;
  nameAr: string;
  nameEn: string;
  slug: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  pricingModel: string;
  requiresSpecialty: boolean;
  sortOrder: number;
  isActive: boolean;
  variants: AdminServiceVariantDto[];
}

export interface AdminServiceCategoryDto {
  id: number;
  nameAr: string;
  nameEn: string;
  slug: string;
  iconKey: string | null;
  sortOrder: number;
  isActive: boolean;
  services: AdminServiceCatalogItemDto[];
}

export async function getCategories(): Promise<AdminServiceCategoryDto[]> {
  const { data } = await api.get<AdminServiceCategoryDto[]>("/api/v1/admin/catalog/categories");
  return data;
}

export async function createCategory(body: { nameAr: string; nameEn: string; slug: string; iconKey: string | null; sortOrder: number }) {
  const { data } = await api.post<number>("/api/v1/admin/catalog/categories", body);
  return data;
}

export async function updateCategory(id: number, body: { nameAr: string; nameEn: string; iconKey: string | null; sortOrder: number; isActive: boolean }) {
  await api.put(`/api/v1/admin/catalog/categories/${id}`, body);
}

export async function updateService(
  id: number,
  body: { nameAr: string; nameEn: string; descriptionAr: string | null; descriptionEn: string | null; requiresSpecialty: boolean; sortOrder: number; isActive: boolean },
) {
  await api.put(`/api/v1/admin/catalog/services/${id}`, body);
}

export async function updateVariant(
  id: number,
  body: {
    nameAr: string;
    nameEn: string;
    basePrice: number;
    requiresQuantity: boolean;
    includedQuantity: number;
    extraUnitPrice: number;
    quantityLabelAr: string | null;
    quantityLabelEn: string | null;
    minQuantity: number;
    maxQuantity: number;
    sortOrder: number;
    isActive: boolean;
  },
) {
  await api.put(`/api/v1/admin/catalog/variants/${id}`, body);
}
