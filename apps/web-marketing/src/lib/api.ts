const API_BASE_URL = import.meta.env.PUBLIC_API_BASE_URL ?? "http://localhost:5280";

export interface ServiceDto {
  id: number;
  nameAr: string;
  nameEn: string;
  slug: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  pricingModel: string;
  requiresSpecialty: boolean;
}

export interface ServiceCategoryDto {
  id: number;
  nameAr: string;
  nameEn: string;
  slug: string;
  iconKey: string;
  services: ServiceDto[];
}

/** Fetched once at build time (SSG) — the marketing site never calls the API at request time. */
export async function fetchServiceCategories(): Promise<ServiceCategoryDto[]> {
  const res = await fetch(`${API_BASE_URL}/api/v1/catalog/categories`);
  if (!res.ok) {
    throw new Error(`Failed to fetch service categories: ${res.status}`);
  }
  return res.json();
}

export const CATEGORY_COLOR_CLASS: Record<string, { bg: string; fg: string }> = {
  consultations: { bg: "var(--color-cat-consult-bg)", fg: "var(--color-cat-consult-fg)" },
  "judiciary-execution": { bg: "var(--color-cat-judiciary-bg)", fg: "var(--color-cat-judiciary-fg)" },
  notarization: { bg: "var(--color-cat-notary-bg)", fg: "var(--color-cat-notary-fg)" },
  business: { bg: "var(--color-cat-business-bg)", fg: "var(--color-cat-business-fg)" },
  other: { bg: "var(--color-cat-other-bg)", fg: "var(--color-cat-other-fg)" },
};
