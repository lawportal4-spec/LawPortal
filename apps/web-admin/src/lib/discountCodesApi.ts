import { api, type PagedResult } from "./api";

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

export type DiscountCodeListStatus = "Active" | "Scheduled" | "Expired" | "UsedUp" | "Inactive";

export interface DiscountCodeFilters {
  search?: string;
  status?: DiscountCodeListStatus;
  scope?: DiscountScope;
  kind?: DiscountKind;
  /** ISO dates: codes whose validity period overlaps this range. */
  validFrom?: string;
  validTo?: string;
  page: number;
  pageSize: number;
}

export async function getDiscountCodes(filters: DiscountCodeFilters): Promise<PagedResult<AdminDiscountCodeDto>> {
  const { data } = await api.get<PagedResult<AdminDiscountCodeDto>>("/api/v1/admin/discount-codes", { params: filters });
  return data;
}

export async function getDiscountCode(id: string): Promise<AdminDiscountCodeDto> {
  const { data } = await api.get<AdminDiscountCodeDto>(`/api/v1/admin/discount-codes/${id}`);
  return data;
}

/** Returns the new code's id. */
export async function createDiscountCode(input: DiscountCodeInput): Promise<string> {
  const { data } = await api.post<string>("/api/v1/admin/discount-codes", input);
  return data;
}

/** The code text itself can't change after creation; the API keeps the original. */
export async function updateDiscountCode(id: string, input: DiscountCodeInput): Promise<void> {
  await api.put(`/api/v1/admin/discount-codes/${id}`, input);
}

export const EMPTY_DISCOUNT_CODE: DiscountCodeInput = {
  code: "",
  descriptionAr: null,
  descriptionEn: null,
  kind: "Percentage",
  value: 10,
  maxDiscountAmount: null,
  minAmount: null,
  scopes: [],
  usageLimit: null,
  perUserLimit: 1,
  firstPaymentOnly: false,
  isActive: true,
  startsAtUtc: null,
  endsAtUtc: null,
};

/** Same precedence as the API's status filter: off, expired, used up, scheduled, active.
 * `tag` is the raw value StatusTag colours by. */
export function statusOf(c: AdminDiscountCodeDto): { status: DiscountCodeListStatus; tag: string } {
  const now = Date.now();
  if (!c.isActive) return { status: "Inactive", tag: "Inactive" };
  if (c.endsAtUtc && Date.parse(c.endsAtUtc) <= now) return { status: "Expired", tag: "Expired" };
  if (c.usageLimit != null && c.used >= c.usageLimit) return { status: "UsedUp", tag: "Withdrawn" };
  if (c.startsAtUtc && Date.parse(c.startsAtUtc) > now) return { status: "Scheduled", tag: "Pending" };
  return { status: "Active", tag: "Active" };
}

export async function getDiscountRedemptions(id: string): Promise<DiscountRedemptionDto[]> {
  const { data } = await api.get<DiscountRedemptionDto[]>(`/api/v1/admin/discount-codes/${id}/redemptions`);
  return data;
}
