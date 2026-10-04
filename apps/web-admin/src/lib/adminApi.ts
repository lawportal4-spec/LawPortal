import { api } from "./api";

export interface RequestStatusCountDto {
  status: string;
  count: number;
}

export interface SubscriptionPlanCountDto {
  planNameEn: string;
  count: number;
}

export interface AdminDashboardDto {
  totalClients: number;
  totalVerifiedLawyers: number;
  pendingLawyerVerifications: number;
  totalAdmins: number;
  requestsByStatus: RequestStatusCountDto[];
  commissionRevenueThisMonth: number;
  subscriptionRevenueThisMonth: number;
  ledgerIsBalanced: boolean;
  activeSubscriptionsByPlan: SubscriptionPlanCountDto[];
}

export async function getDashboard(): Promise<AdminDashboardDto> {
  const { data } = await api.get<AdminDashboardDto>("/api/v1/admin/dashboard");
  return data;
}

export interface PendingLawyerDto {
  lawyerProfileId: string;
  userId: string;
  fullName: string;
  email: string;
  phoneE164: string | null;
  licenseNumber: string;
  licenseType: "Licensed" | "Trainee";
  issueDate: string;
  expiryDate: string;
  verificationStatus: string;
  /** Short-lived signed URL; null for lawyers who registered before the upload existed. */
  licenseDocumentUrl: string | null;
}

export async function getPendingLawyers(): Promise<PendingLawyerDto[]> {
  const { data } = await api.get<PendingLawyerDto[]>("/api/v1/admin/lawyers/pending");
  return data;
}

export async function verifyLawyer(lawyerProfileId: string): Promise<void> {
  await api.post(`/api/v1/admin/lawyers/${lawyerProfileId}/verify`);
}

export async function rejectLawyer(lawyerProfileId: string, reason: string): Promise<void> {
  await api.post(`/api/v1/admin/lawyers/${lawyerProfileId}/reject`, { reason });
}
