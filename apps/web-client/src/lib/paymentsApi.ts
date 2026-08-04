import { api } from "./api";

export interface CheckoutResultDto {
  paymentId: string;
  number: string;
  status: string;
  redirectUrl: string | null;
  paidImmediately: boolean;
}

export interface InvoiceDto {
  number: string;
  subtotalExVat: number;
  vatAmount: number;
  total: number;
  isVatApplicable: boolean;
  sellerNameAr: string;
  sellerNameEn: string;
  sellerVatNumber: string | null;
  qrPayloadBase64: string;
  issuedAtUtc: string;
}

export interface WalletDto {
  balance: number;
  currencyCode: string;
}

export interface WalletTransactionDto {
  id: string;
  type: "TopUp" | "Payment" | "Refund";
  amount: number;
  description: string | null;
  createdAtUtc: string;
}

export async function checkout(requestId: string, paymentMethod: "Card" | "Wallet"): Promise<CheckoutResultDto> {
  const { data } = await api.post<CheckoutResultDto>("/api/v1/client/payments/checkout", { requestId, paymentMethod });
  return data;
}

export async function getInvoice(requestId: string): Promise<InvoiceDto> {
  const { data } = await api.get<InvoiceDto>(`/api/v1/client/requests/${requestId}/invoice`);
  return data;
}

export async function getWallet(): Promise<WalletDto> {
  const { data } = await api.get<WalletDto>("/api/v1/client/wallet");
  return data;
}

export async function getWalletTransactions(): Promise<WalletTransactionDto[]> {
  const { data } = await api.get<WalletTransactionDto[]>("/api/v1/client/wallet/transactions");
  return data;
}

export async function topUpWallet(amount: number): Promise<CheckoutResultDto> {
  const { data } = await api.post<CheckoutResultDto>("/api/v1/client/wallet/topup", { amount });
  return data;
}
