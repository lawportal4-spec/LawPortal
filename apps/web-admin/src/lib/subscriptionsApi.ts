import { api } from "./api";

export interface AdminSubscriptionPlanDto {
  id: number;
  slug: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  monthlyPrice: number;
  commissionPercentageOverride: number | null;
  includesBroadcastBidding: boolean;
  sortOrder: number;
  isActive: boolean;
  activeSubscriberCount: number;
}

export async function getAdminSubscriptionPlans(): Promise<AdminSubscriptionPlanDto[]> {
  const { data } = await api.get<AdminSubscriptionPlanDto[]>("/api/v1/admin/subscription-plans");
  return data;
}

export async function updateSubscriptionPlan(plan: AdminSubscriptionPlanDto): Promise<void> {
  await api.put(`/api/v1/admin/subscription-plans/${plan.id}`, {
    id: plan.id,
    nameAr: plan.nameAr,
    nameEn: plan.nameEn,
    descriptionAr: plan.descriptionAr,
    descriptionEn: plan.descriptionEn,
    monthlyPrice: plan.monthlyPrice,
    commissionPercentageOverride: plan.commissionPercentageOverride,
    includesBroadcastBidding: plan.includesBroadcastBidding,
    sortOrder: plan.sortOrder,
    isActive: plan.isActive,
  });
}
