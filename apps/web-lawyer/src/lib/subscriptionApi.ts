import { api } from "./api";

export interface SubscriptionPlanDto {
  id: number;
  slug: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  monthlyPrice: number;
  commissionPercentageOverride: number | null;
  includesBroadcastBidding: boolean;
}

export interface MySubscriptionDto {
  effectivePlan: SubscriptionPlanDto;
  subscriptionId: string | null;
  status: "Active" | "PastDue" | "Canceled" | "Expired" | null;
  currentPeriodStartUtc: string | null;
  currentPeriodEndUtc: string | null;
  cancelAtPeriodEnd: boolean;
}

export interface SubscriptionInvoiceDto {
  id: string;
  number: string;
  periodStartUtc: string;
  periodEndUtc: string;
  subtotalExVat: number;
  vatAmount: number;
  total: number;
  status: "Pending" | "Paid" | "Failed";
  dueAtUtc: string;
  paidAtUtc: string | null;
}

export interface CheckoutResultDto {
  paymentId: string;
  number: string;
  status: string;
  redirectUrl: string | null;
  paidImmediately: boolean;
}

export async function getSubscriptionPlans(): Promise<SubscriptionPlanDto[]> {
  const { data } = await api.get<SubscriptionPlanDto[]>("/api/v1/lawyer/subscription/plans");
  return data;
}

export async function getMySubscription(): Promise<MySubscriptionDto> {
  const { data } = await api.get<MySubscriptionDto>("/api/v1/lawyer/subscription");
  return data;
}

export async function getMySubscriptionInvoices(): Promise<SubscriptionInvoiceDto[]> {
  const { data } = await api.get<SubscriptionInvoiceDto[]>("/api/v1/lawyer/subscription/invoices");
  return data;
}

export async function subscribeToPlan(planId: number): Promise<SubscriptionInvoiceDto> {
  const { data } = await api.post<SubscriptionInvoiceDto>("/api/v1/lawyer/subscription/subscribe", { planId });
  return data;
}

export async function cancelSubscription(): Promise<void> {
  await api.post("/api/v1/lawyer/subscription/cancel");
}

export async function payInvoice(invoiceId: string): Promise<CheckoutResultDto> {
  const { data } = await api.post<CheckoutResultDto>(`/api/v1/lawyer/subscription/invoices/${invoiceId}/pay`);
  return data;
}
