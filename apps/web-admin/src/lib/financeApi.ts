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

export const REFUND_REASONS = [
  "ServiceNotDelivered", "LawyerNoResponse", "LawyerDeclined", "ClientCancelled",
  "DuplicatePayment", "TechnicalIssue", "QualityComplaint", "Other",
] as const;
export type RefundReason = (typeof REFUND_REASONS)[number];

export interface RefundLineDto {
  id: string;
  amount: number;
  reason: string;
  details: string | null;
  status: string;
  gatewayRefundId: string | null;
  lawyerPortion: number;
  vatPortion: number;
  commissionPortion: number;
  discountSupportPortion: number;
  /** Set when the refund came after the lawyer was paid: who covered their share. */
  lawyerShareBearer: RefundBearer | null;
  createdAtUtc: string;
}

export interface PayoutLineDto {
  id: string;
  amount: number;
  status: string;
  createdAtUtc: string;
  releasedAtUtc: string | null;
  /** Deducted at release for an earlier debt; the lawyer was paid amount − debtOffset. */
  debtOffset: number;
  lawyerProfileId: string;
  /** What this lawyer owes the platform right now (all payments). */
  lawyerDebtBalance: number;
}

export type RefundBearer = "Lawyer" | "Platform";

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
  grossAmount: number;
  discountAmount: number;
  transaction: GatewayTransactionDto | null;
  walletTransactionId: string | null;
  discount: PaymentDiscountDto | null;
  discountSupport: number;
  requestType: string | null;
  /** Only once the lawyer has been paid: refunds allowed until deadlineUtc. */
  refundWindow: { windowDays: number; deadlineUtc: string; expired: boolean } | null;
  lawyerProfileId: string | null;
  clientPhoneE164: string | null;
  clientProfileId: string;
  serviceRequestId: string | null;
}

export interface PaymentDiscountDto {
  id: string;
  code: string;
  kind: "Percentage" | "Fixed";
  value: number;
  maxDiscountAmount: number | null;
  scopes: string[];
}

export interface GatewayTransactionDto {
  transactionId: string | null;
  sourceType: string | null;
  cardBrand: string | null;
  cardMasked: string | null;
  referenceNumber: string | null;
  authorizationCode: string | null;
  responseCode: string | null;
  message: string | null;
  fee: number | null;
  fetchedAtUtc: string;
}

export interface PaymentFilters {
  status?: string;
  search?: string;
  purpose?: string;
  method?: string;
  from?: string;
  to?: string;
  minTotal?: number;
  maxTotal?: number;
  hasDiscount?: boolean;
  payout?: string;
  page: number;
  pageSize: number;
}

export async function getPayments(filters: PaymentFilters): Promise<PagedResult<AdminPaymentSummaryDto>> {
  const { data } = await api.get<PagedResult<AdminPaymentSummaryDto>>("/api/v1/admin/payments", { params: filters });
  return data;
}

export async function getPaymentDetail(id: string): Promise<AdminPaymentDetailDto> {
  const { data } = await api.get<AdminPaymentDetailDto>(`/api/v1/admin/payments/${id}`);
  return data;
}

export async function refundPayment(id: string, amount: number, reason: RefundReason, details: string, lawyerShareBearer?: RefundBearer): Promise<void> {
  await api.post(`/api/v1/admin/payments/${id}/refund`, { amount, reason, details: details.trim() || null, lawyerShareBearer });
}

export async function releasePayout(payoutId: string, reason: string): Promise<void> {
  await api.post(`/api/v1/admin/payments/payouts/${payoutId}/release`, { reason });
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

export async function getLedgerSummary(from?: string, to?: string): Promise<LedgerSummaryDto> {
  const { data } = await api.get<LedgerSummaryDto>("/api/v1/admin/finance/ledger/summary", { params: { from, to } });
  return data;
}

export const JOURNAL_KINDS = ["Checkout", "TopUp", "Refund", "Payout", "DebtRepayment", "Subscription", "RegistrationFee"] as const;
export const LEDGER_ACCOUNTS = [
  "ClearingGateway", "EscrowPayable", "VatPayable", "WalletLiability",
  "CommissionRevenue", "SubscriptionRevenue", "RegistrationFeeRevenue", "DiscountExpense",
  "LawyerReceivable", "RefundLossExpense",
] as const;
export type LedgerAccount = (typeof LEDGER_ACCOUNTS)[number];

export interface JournalLineDto {
  account: LedgerAccount;
  isDebit: boolean;
  amount: number;
  description: string | null;
}

export interface JournalEntryDto {
  referenceType: string;
  referenceId: string;
  kind: string;
  number: string | null;
  paymentId: string | null;
  createdAtUtc: string;
  lines: JournalLineDto[];
}

export interface JournalFilters {
  search?: string;
  kind?: string;
  account?: string;
  from?: string;
  to?: string;
  page: number;
  pageSize: number;
}

export async function getLedgerJournal(filters: JournalFilters): Promise<PagedResult<JournalEntryDto>> {
  const { data } = await api.get<PagedResult<JournalEntryDto>>("/api/v1/admin/finance/ledger/journal", { params: filters });
  return data;
}

export interface CommissionPolicyDto {
  id: number;
  serviceCategorySlug: string | null;
  percentage: number;
  isActive: boolean;
  effectiveFromUtc: string;
  state: "Applied" | "Scheduled" | "Stopped";
}

export async function getCommissionPolicies(): Promise<CommissionPolicyDto[]> {
  const { data } = await api.get<CommissionPolicyDto[]>("/api/v1/admin/finance/commission-policies");
  return data;
}

export async function createCommissionPolicy(serviceCategorySlug: string | null, percentage: number, effectiveFromUtc?: string): Promise<number> {
  const { data } = await api.post<number>("/api/v1/admin/finance/commission-policies", { serviceCategorySlug, percentage, effectiveFromUtc });
  return data;
}
