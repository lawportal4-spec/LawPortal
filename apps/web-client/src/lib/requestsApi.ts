import { api, type PagedResult } from "./api";

export interface RequestSummaryDto {
  id: string;
  number: string;
  kind: "Consultation" | "Catalog" | "Bidding";
  serviceNameAr: string;
  serviceNameEn: string;
  status: string;
  title: string | null;
  subtotal: number | null;
  createdAtUtc: string;
}

export interface AttachmentDto {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  scanStatus: string;
}

export interface RequestDetailDto {
  id: string;
  number: string;
  kind: "Consultation" | "Catalog" | "Bidding";
  serviceNameAr: string;
  serviceNameEn: string;
  status: string;
  specialtyNameAr: string | null;
  specialtyNameEn: string | null;
  title: string | null;
  description: string | null;
  subtotal: number | null;
  currencyCode: string;
  createdAtUtc: string;
  submittedAtUtc: string | null;
  cancelledAtUtc: string | null;
  cancelReason: string | null;
  consultationType: string | null;
  lawyerProfileId: string | null;
  lawyerFullName: string | null;
  scheduledStartUtc: string | null;
  voiceNoteDurationSeconds: number | null;
  variantNameAr: string | null;
  variantNameEn: string | null;
  quantity: number | null;
  sendMethod: "Broadcast" | "Targeted" | null;
  awardedLawyerProfileId: string | null;
  awardedLawyerFullName: string | null;
  offerCount: number | null;
  attachments: AttachmentDto[];
}

export interface RequestListParams {
  status?: string;
  kind?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export async function listMyRequests(params: RequestListParams): Promise<PagedResult<RequestSummaryDto>> {
  const { data } = await api.get<PagedResult<RequestSummaryDto>>("/api/v1/client/requests", { params });
  return data;
}

export async function getRequestDetail(id: string): Promise<RequestDetailDto> {
  const { data } = await api.get<RequestDetailDto>(`/api/v1/client/requests/${id}`);
  return data;
}
