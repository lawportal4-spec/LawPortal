import { api, type PagedResult } from "./api";

export interface AdminPaymentSummaryDto {
  id: string;
  number: string;
  purpose: string;
  status: string;
  total: number;
  clientName: string | null;
  lawyerName: string | null;
  requestNumber: string | null;
  createdAtUtc: string;
  paidAtUtc: string | null;
}

export interface RefundLineDto {
  id: string;
  amount: number;
  reason: string;
  status: string;
  createdAtUtc: string;
}

export interface PayoutLineDto {
  id: string;
  amount: number;
  status: string;
  createdAtUtc: string;
  releasedAtUtc: string | null;
}

export interface AdminPaymentDetailDto {
  id: string;
  number: string;
  purpose: string;
  status: string;
  methodDescription: string;
  total: number;
  vatAmount: number;
  commissionAmount: number;
  netToLawyerAmount: number;
  isVatApplicable: boolean;
  clientName: string | null;
  lawyerName: string | null;
  requestNumber: string | null;
  gatewayProvider: string;
  gatewayPaymentId: string | null;
  failureReason: string | null;
  createdAtUtc: string;
  paidAtUtc: string | null;
  refunds: RefundLineDto[];
  payout: PayoutLineDto | null;
}

export async function getPayments(status?: string, page = 1, pageSize = 20): Promise<PagedResult<AdminPaymentSummaryDto>> {
  const { data } = await api.get<PagedResult<AdminPaymentSummaryDto>>("/api/v1/admin/payments", { params: { status, page, pageSize } });
  return data;
}

export async function getPaymentDetail(id: string): Promise<AdminPaymentDetailDto> {
  const { data } = await api.get<AdminPaymentDetailDto>(`/api/v1/admin/payments/${id}`);
  return data;
}

export async function refundPayment(id: string, amount: number, reason: string): Promise<void> {
  await api.post(`/api/v1/admin/payments/${id}/refund`, { amount, reason });
}

export async function releasePayout(payoutId: string): Promise<void> {
  await api.post(`/api/v1/admin/payments/payouts/${payoutId}/release`);
}

export interface LedgerAccountBalanceDto {
  account: string;
  totalDebits: number;
  totalCredits: number;
  netBalance: number;
}

export interface LedgerSummaryDto {
  accounts: LedgerAccountBalanceDto[];
  grandTotalDebits: number;
  grandTotalCredits: number;
  isBalanced: boolean;
}

export async function getLedgerSummary(): Promise<LedgerSummaryDto> {
  const { data } = await api.get<LedgerSummaryDto>("/api/v1/admin/finance/ledger/summary");
  return data;
}

export interface LedgerEntryDto {
  id: string;
  account: string;
  isDebit: boolean;
  amount: number;
  referenceType: string;
  referenceId: string;
  description: string | null;
  createdAtUtc: string;
}

export async function getLedgerEntries(page = 1, pageSize = 25): Promise<PagedResult<LedgerEntryDto>> {
  const { data } = await api.get<PagedResult<LedgerEntryDto>>("/api/v1/admin/finance/ledger/entries", { params: { page, pageSize } });
  return data;
}

export interface CommissionPolicyDto {
  id: number;
  serviceCategorySlug: string | null;
  percentage: number;
  isActive: boolean;
  effectiveFromUtc: string;
}

export async function getCommissionPolicies(): Promise<CommissionPolicyDto[]> {
  const { data } = await api.get<CommissionPolicyDto[]>("/api/v1/admin/finance/commission-policies");
  return data;
}

export async function createCommissionPolicy(serviceCategorySlug: string | null, percentage: number): Promise<number> {
  const { data } = await api.post<number>("/api/v1/admin/finance/commission-policies", { serviceCategorySlug, percentage });
  return data;
}
