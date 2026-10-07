import { api, type PagedResult } from "./api";

export interface LawyerDashboardDto {
  awaitingAcceptance: number;
  inProgress: number;
  completedThisMonth: number;
  unreadMessageThreads: number;
  earningsHeld: number;
  earningsReleasedThisMonth: number;
  avgRating: number;
  ratingCount: number;
}

export interface LawyerRequestSummaryDto {
  id: string;
  number: string;
  clientName: string;
  serviceNameAr: string;
  serviceNameEn: string;
  status: string;
  title: string | null;
  subtotal: number | null;
  createdAtUtc: string;
}

export interface LawyerRequestDetailDto {
  id: string;
  number: string;
  clientName: string;
  serviceNameAr: string;
  serviceNameEn: string;
  status: string;
  specialtyNameAr: string | null;
  specialtyNameEn: string | null;
  consultationType: string | null;
  scheduledStartUtc: string | null;
  selectedDurationMinutes: number | null;
  title: string | null;
  description: string | null;
  subtotal: number | null;
  currencyCode: string;
  createdAtUtc: string;
  submittedAtUtc: string | null;
}

export interface LawyerProfileEditDto {
  bioAr: string | null;
  bioEn: string | null;
  acceptingNewRequests: boolean;
  specialtyIds: number[];
  languageIds: number[];
  /** null until prices have been saved once. */
  pricing: { writtenPrice: number; price15: number; price30: number; price45: number } | null;
}

export interface PayoutSummaryDto {
  id: string;
  requestNumber: string;
  amount: number;
  status: string;
  createdAtUtc: string;
  releasedAtUtc: string | null;
  /** Deducted at release to settle a debt to the platform; paid = amount − debtOffset. */
  debtOffset: number;
}

export interface MyDebtDto {
  balance: number;
  entries: { kind: string; amount: number; requestNumber: string | null; createdAtUtc: string }[];
}

export async function getMyDebt(): Promise<MyDebtDto> {
  const { data } = await api.get<MyDebtDto>("/api/v1/lawyer/debt");
  return data;
}

export interface LawyerReviewDto {
  id: string;
  requestNumber: string;
  rating: number;
  comment: string | null;
  createdAtUtc: string;
}

export interface SpecialtyDto {
  id: number;
  nameAr: string;
  nameEn: string;
  slug: string;
}

export interface LanguageDto {
  id: number;
  nameAr: string;
  nameEn: string;
  code: string;
}

export async function getDashboard(): Promise<LawyerDashboardDto> {
  const { data } = await api.get<LawyerDashboardDto>("/api/v1/lawyer/dashboard");
  return data;
}

export async function listIncomingRequests(status?: string, page = 1, pageSize = 10): Promise<PagedResult<LawyerRequestSummaryDto>> {
  const { data } = await api.get<PagedResult<LawyerRequestSummaryDto>>("/api/v1/lawyer/requests", {
    params: { status, page, pageSize },
  });
  return data;
}

export async function getIncomingRequestDetail(id: string): Promise<LawyerRequestDetailDto> {
  const { data } = await api.get<LawyerRequestDetailDto>(`/api/v1/lawyer/requests/${id}`);
  return data;
}

export async function acceptRequest(id: string): Promise<void> {
  await api.post(`/api/v1/lawyer/requests/${id}/accept`);
}

export async function declineRequest(id: string, reason: string): Promise<void> {
  await api.post(`/api/v1/lawyer/requests/${id}/decline`, { reason });
}

export async function completeRequest(id: string): Promise<void> {
  await api.post(`/api/v1/lawyer/requests/${id}/complete`);
}

export async function getMyProfile(): Promise<LawyerProfileEditDto> {
  const { data } = await api.get<LawyerProfileEditDto>("/api/v1/lawyer/profile");
  return data;
}

export async function updateMyProfile(body: Omit<LawyerProfileEditDto, "pricing">): Promise<void> {
  await api.put("/api/v1/lawyer/profile", body);
}

export async function updatePricing(body: { writtenPrice: number; price15: number; price30: number; price45: number }): Promise<void> {
  await api.put("/api/v1/lawyer/pricing", body);
}

export async function renewLicense(body: { licenseNumber: string; issueDate: string; expiryDate: string }): Promise<void> {
  await api.post("/api/v1/lawyer/license/renew", body);
}

export async function getMyEarnings(): Promise<PayoutSummaryDto[]> {
  const { data } = await api.get<PayoutSummaryDto[]>("/api/v1/lawyer/earnings");
  return data;
}

export async function getMyReviews(): Promise<LawyerReviewDto[]> {
  const { data } = await api.get<LawyerReviewDto[]>("/api/v1/lawyer/reviews");
  return data;
}

export async function getSpecialties(): Promise<SpecialtyDto[]> {
  const { data } = await api.get<SpecialtyDto[]>("/api/v1/catalog/specialties");
  return data;
}

export async function getLanguages(): Promise<LanguageDto[]> {
  const { data } = await api.get<LanguageDto[]>("/api/v1/catalog/languages");
  return data;
}
