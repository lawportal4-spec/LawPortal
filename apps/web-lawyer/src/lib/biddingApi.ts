import { api, type PagedResult } from "./api";
import type { LawyerRequestSummaryDto } from "./lawyerApi";

export interface BiddingFeedItemDto {
  id: string;
  number: string;
  serviceNameAr: string;
  serviceNameEn: string;
  specialtyNameAr: string | null;
  specialtyNameEn: string | null;
  title: string | null;
  invitedAtUtc: string;
  myOfferStatus: string | null;
  myLatestOfferAmount: number | null;
  createdAtUtc: string;
}

export interface BiddingRequestDetailForLawyerDto {
  id: string;
  number: string;
  serviceNameAr: string;
  serviceNameEn: string;
  specialtyNameAr: string | null;
  specialtyNameEn: string | null;
  title: string | null;
  description: string | null;
  status: string;
  attachmentsUnlocked: boolean;
  attachmentFileNames: string[];
  myOfferStatus: string | null;
  myOfferId: string | null;
  myLatestOfferAmount: number | null;
}

export async function getBiddingFeed(page = 1, pageSize = 10): Promise<PagedResult<BiddingFeedItemDto>> {
  const { data } = await api.get<PagedResult<BiddingFeedItemDto>>("/api/v1/lawyer/bidding/feed", {
    params: { page, pageSize },
  });
  return data;
}

export async function getBiddingRequestDetail(id: string): Promise<BiddingRequestDetailForLawyerDto> {
  const { data } = await api.get<BiddingRequestDetailForLawyerDto>(`/api/v1/lawyer/bidding/${id}`);
  return data;
}

export async function submitOffer(id: string, amount: number, message?: string): Promise<string> {
  const { data } = await api.post<string>(`/api/v1/lawyer/bidding/${id}/offer`, { amount, message });
  return data;
}

export async function withdrawOffer(offerId: string): Promise<void> {
  await api.post(`/api/v1/lawyer/bidding/offers/${offerId}/withdraw`);
}

export async function getAwardedBiddingRequests(
  status?: string,
  page = 1,
  pageSize = 10,
): Promise<PagedResult<LawyerRequestSummaryDto>> {
  const { data } = await api.get<PagedResult<LawyerRequestSummaryDto>>("/api/v1/lawyer/bidding/awarded", {
    params: { status, page, pageSize },
  });
  return data;
}
