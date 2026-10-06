import { api } from "./api";

export const DISCOUNT_SCOPES = [
  "InstantConsultation",
  "ScheduledConsultation",
  "WrittenConsultation",
  "BiddingRequest",
  "CatalogService",
  "LawyerSubscription",
  "LawyerRegistrationFee",
] as const;
export type DiscountScope = (typeof DISCOUNT_SCOPES)[number];
export type DiscountKind = "Percentage" | "Fixed";

export interface DiscountCodeInput {
  code: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  kind: DiscountKind;
  value: number;
  maxDiscountAmount: number | null;
  minAmount: number | null;
  scopes: DiscountScope[];
  usageLimit: number | null;
  perUserLimit: number | null;
  firstPaymentOnly: boolean;
  isActive: boolean;
  startsAtUtc: string | null;
  endsAtUtc: string | null;
}

export interface AdminDiscountCodeDto extends DiscountCodeInput {
  id: string;
  used: number;
  totalDiscounted: number;
  createdAtUtc: string;
}

export interface DiscountRedemptionDto {
  id: string;
  userName: string;
  scope: DiscountScope;
  referenceId: string;
  amount: number;
  status: "Pending" | "Confirmed" | "Released";
  createdAtUtc: string;
}

export async function getDiscountCodes(): Promise<AdminDiscountCodeDto[]> {
  const { data } = await api.get<AdminDiscountCodeDto[]>("/api/v1/admin/discount-codes");
  return data;
}

export async function saveDiscountCode(id: string | null, input: DiscountCodeInput): Promise<void> {
  if (id) await api.put(`/api/v1/admin/discount-codes/${id}`, input);
  else await api.post("/api/v1/admin/discount-codes", input);
}

export async function getDiscountRedemptions(id: string): Promise<DiscountRedemptionDto[]> {
  const { data } = await api.get<DiscountRedemptionDto[]>(`/api/v1/admin/discount-codes/${id}/redemptions`);
  return data;
}
