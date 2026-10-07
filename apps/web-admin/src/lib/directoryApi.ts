import { api, type PagedResult } from "./api";

export type AccountStatus = "Active" | "Suspended" | "Deleted" | "Pending";

export interface ActivityItem { action: string; details: string | null; actorRole: string | null; actorName: string | null; occurredAtUtc: string }

export interface AdminClientSummary {
  clientProfileId: string; userId: string; fullName: string | null; phoneE164: string | null;
  cityNameAr: string | null; cityNameEn: string | null; requestsCount: number; totalPaid: number; walletBalance: number;
  lastActivityUtc: string | null; status: AccountStatus; joinedAtUtc: string;
}

export interface AdminRequestRow {
  id: string; number: string; type: string; lawyerName: string | null; lawyerProfileId: string | null;
  clientName: string | null; clientPhoneE164: string | null; clientProfileId: string; amount: number | null; status: string; createdAtUtc: string;
}

export interface AdminClientDetail {
  summary: AdminClientSummary; pledgeAccepted: boolean; pledgeAcceptedAtUtc: string | null; totalRefunded: number;
  requests: AdminRequestRow[];
  payments: { id: string; number: string; purpose: string; method: string; total: number; refunded: number; status: string; createdAtUtc: string }[];
  wallet: { type: string; amount: number; paymentNumber: string | null; paymentId: string | null; description: string | null; createdAtUtc: string }[];
  activity: ActivityItem[];
}

export interface AdminRequestDetail {
  summary: AdminRequestRow; title: string | null; description: string | null;
  categoryNameAr: string | null; categoryNameEn: string | null; serviceNameAr: string | null; serviceNameEn: string | null;
  scheduledStartUtc: string | null; cancelReason: string | null; offersCount: number;
  payments: { id: string; number: string; gross: number; discount: number; total: number; refunded: number; status: string; method: string;
    createdAtUtc: string; lawyerShare: number | null; lawyerPaidOut: number | null; payoutStatus: string | null }[];
  history: { from: string | null; to: string; trigger: string | null; occurredAtUtc: string }[];
  attachments: { id: string; fileName: string; scanStatus: string; url: string | null }[];
  review: { rating: number; comment: string | null; createdAtUtc: string } | null;
  reports: { reason: string; details: string | null; createdAtUtc: string }[];
  messagesCount: number;
  activity: ActivityItem[];
}

export interface LawyerFinance {
  totals: { earned: number; paidOut: number; held: number; suspended: number; debt: number; balance: number };
  months: { year: number; month: number; paidOut: number; held: number; deducted: number }[];
  statement: { kind: string; amount: number; runningBalance: number; reference: string | null; paymentId: string | null; requestId: string | null; note: string | null; occurredAtUtc: string }[];
  accountStatus: AccountStatus;
  activity: ActivityItem[];
}

export interface SearchHit { kind: "Client" | "Lawyer" | "Request" | "Payment"; id: string; title: string; subtitle: string | null }

export async function getClients(params: { search?: string; regionId?: number; cityId?: number; status?: string; hasWalletBalance?: boolean; page: number; pageSize: number }) {
  const { data } = await api.get<{ stats: { total: number; activeThisMonth: number; walletTotal: number; totalPaid: number }; page: PagedResult<AdminClientSummary> }>("/api/v1/admin/clients", { params });
  return data;
}
export async function getClient(id: string) {
  const { data } = await api.get<AdminClientDetail>(`/api/v1/admin/clients/${id}`);
  return data;
}
export async function getRequests(params: { search?: string; type?: string; status?: string; from?: string; to?: string; lawyerProfileId?: string; page: number; pageSize: number }) {
  const { data } = await api.get<{ stats: { total: number; inProgress: number; completedThisMonth: number; withReports: number }; page: PagedResult<AdminRequestRow> }>("/api/v1/admin/requests", { params });
  return data;
}
export async function getRequest(id: string) {
  const { data } = await api.get<AdminRequestDetail>(`/api/v1/admin/requests/${id}`);
  return data;
}
export async function getRequestChat(id: string) {
  const { data } = await api.get<{ senderRole: "Client" | "Lawyer"; senderName: string | null; body: string; sentAtUtc: string }[]>(`/api/v1/admin/requests/${id}/chat`);
  return data;
}
export async function getLawyerFinance(id: string) {
  const { data } = await api.get<LawyerFinance>(`/api/v1/admin/lawyers/${id}/finance`);
  return data;
}
export async function setSuspended(userId: string, suspended: boolean, reason: string) {
  await api.post(`/api/v1/admin/accounts/${userId}/suspension`, { suspended, reason: reason.trim() });
}
export async function getNotes(entityType: "Client" | "Lawyer" | "Request", id: string) {
  const { data } = await api.get<{ id: string; body: string; authorName: string | null; createdAtUtc: string }[]>(`/api/v1/admin/notes/${entityType}/${id}`);
  return data;
}
export async function addNote(entityType: "Client" | "Lawyer" | "Request", id: string, body: string) {
  await api.post(`/api/v1/admin/notes/${entityType}/${id}`, { body: body.trim() });
}
export async function searchAll(q: string) {
  const { data } = await api.get<SearchHit[]>("/api/v1/admin/search", { params: { q } });
  return data;
}

export const ACCOUNT_STATUS_PILL: Record<AccountStatus, string> = {
  Active: "bg-success-tint text-success",
  Suspended: "bg-warning-tint text-warning",
  Deleted: "bg-surface-raised text-ink-faint",
  Pending: "bg-info-tint text-info",
};

export type PageStatsPage = "lawyers" | "payments" | "catalog" | "discountCodes" | "users" | "audit";
export interface PageStat { key: string; value: number; isMoney: boolean; tone: "gold" | "success" | "danger" | null }

export async function getPageStats(page: PageStatsPage) {
  return (await api.get<PageStat[]>(`/api/v1/admin/page-stats/${page}`)).data;
}
