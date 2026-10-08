import { api } from "./api";

export type DebtCollection = "Offsetting" | "NoUpcomingPayouts" | "LeftPlatform" | "Settled";

export const COLLECTION_PILL: Record<DebtCollection, string> = {
  Offsetting: "bg-info-tint text-info",
  NoUpcomingPayouts: "bg-warning-tint text-warning",
  LeftPlatform: "bg-rubric-tint text-rubric",
  Settled: "bg-success-tint text-success",
};

export interface LawyerDebtSummary {
  lawyerProfileId: string;
  fullName: string;
  accountDeleted: boolean;
  deletedAtUtc: string | null;
  balance: number;
  oldestOpenDebtAtUtc: string | null;
  paymentsCount: number;
  heldPayoutsTotal: number;
  lastReminderAtUtc: string | null;
  collection: DebtCollection;
}

export interface LawyerDebtList {
  stats: { totalOutstanding: number; lawyersOwing: number; collectedThisMonth: number; leftPlatformCount: number; leftPlatformAmount: number };
  items: LawyerDebtSummary[];
}

export interface LawyerDebtDetail {
  summary: LawyerDebtSummary;
  totalDebt: number;
  totalCollected: number;
  contactEmail: string | null;
  contactPhone: string | null;
  suspendedPayoutsTotal: number;
  suspendedPayoutsCount: number;
  entries: { id: string; kind: string; amount: number; paymentNumber: string | null; paymentId: string | null; reference: string | null; note: string | null; createdAtUtc: string }[];
}

export async function getLawyerDebts(collection?: string): Promise<LawyerDebtList> {
  const { data } = await api.get<LawyerDebtList>("/api/v1/admin/lawyer-debts", { params: { collection } });
  return data;
}

export async function getLawyerDebt(id: string): Promise<LawyerDebtDetail> {
  const { data } = await api.get<LawyerDebtDetail>(`/api/v1/admin/lawyer-debts/${id}`);
  return data;
}

export async function recordDebtRepayment(id: string, amount: number, reference: string, note: string): Promise<void> {
  await api.post(`/api/v1/admin/lawyer-debts/${id}/repayments`, { amount, reference: reference.trim(), note: note.trim() || null });
}

export async function sendDebtReminder(id: string): Promise<void> {
  await api.post(`/api/v1/admin/lawyer-debts/${id}/reminders`);
}

export interface RefundPolicy {
  refundWindowDays: number;
  debtReminderIntervalDays: number;
}

export async function getRefundPolicy(): Promise<RefundPolicy> {
  const { data } = await api.get<RefundPolicy>("/api/v1/admin/settings/refund-policy");
  return data;
}

export async function saveRefundPolicy(body: RefundPolicy): Promise<RefundPolicy> {
  const { data } = await api.put<RefundPolicy>("/api/v1/admin/settings/refund-policy", body);
  return data;
}
