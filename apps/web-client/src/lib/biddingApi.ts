import { api } from "./api";

export type BidSendMethod = "Broadcast" | "Targeted";

export interface CreateBiddingDraftParams {
  serviceId: number;
  specialtyId: number;
  subSpecialtyId: number | null;
  sendMethod: BidSendMethod;
  targetedLawyerProfileIds: string[] | null;
}

export async function createBiddingDraft(params: CreateBiddingDraftParams): Promise<string> {
  const { data } = await api.post<string>("/api/v1/client/bidding", params);
  return data;
}

export async function updateBiddingDetails(id: string, title: string, description: string): Promise<void> {
  await api.put(`/api/v1/client/bidding/${id}/details`, { title, description });
}

export interface OfferRevisionDto {
  id: string;
  amount: number;
  proposedBy: "Lawyer" | "Client";
  message: string | null;
  createdAtUtc: string;
}

export interface OfferSummaryDto {
  id: string;
  lawyerProfileId: string;
  lawyerFullName: string;
  lawyerAvgRating: number | null;
  lawyerRatingCount: number;
  lawyerCompletedRequestCount: number;
  status: "Pending" | "Accepted" | "Rejected" | "Withdrawn" | "Expired";
  currentAmount: number;
  expiresAtUtc: string;
  revisions: OfferRevisionDto[];
}

export async function getOfferInbox(requestId: string): Promise<OfferSummaryDto[]> {
  const { data } = await api.get<OfferSummaryDto[]>(`/api/v1/client/bidding/${requestId}/offers`);
  return data;
}

export async function counterOffer(offerId: string, amount: number, message?: string): Promise<void> {
  await api.post(`/api/v1/client/offers/${offerId}/counter`, { amount, message });
}

export async function acceptOffer(offerId: string): Promise<void> {
  await api.post(`/api/v1/client/offers/${offerId}/accept`);
}

export async function rejectOffer(offerId: string): Promise<void> {
  await api.post(`/api/v1/client/offers/${offerId}/reject`);
}
