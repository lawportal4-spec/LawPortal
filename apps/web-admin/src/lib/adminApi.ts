import { api, type PagedResult } from "./api";

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

export type LicenseReviewStatus = "PendingReview" | "ChangesRequested" | "Approved" | "Rejected" | "Expired";
export type LicenseCorrectionIssue = "FileUnreadable" | "WrongFile" | "LicenseNumberMismatch" | "DatesMismatch" | "LicenseTypeMismatch";
export const CORRECTION_ISSUES: LicenseCorrectionIssue[] = [
  "FileUnreadable",
  "WrongFile",
  "LicenseNumberMismatch",
  "DatesMismatch",
  "LicenseTypeMismatch",
];

export interface LawyerRegistrationSummaryDto {
  lawyerProfileId: string;
  fullName: string;
  phoneE164: string | null;
  email: string;
  licenseNumber: string;
  status: LicenseReviewStatus;
  submittedAtUtc: string;
  resubmittedAtUtc: string | null;
}

export interface LawyerRegistrationDetailDto {
  lawyerProfileId: string;
  fullName: string;
  phoneE164: string | null;
  isPhoneVerified: boolean;
  email: string;
  regionNameAr: string | null;
  regionNameEn: string | null;
  cityNameAr: string | null;
  cityNameEn: string | null;
  countryCode: string;
  termsAcceptedAtUtc: string | null;
  submittedAtUtc: string;
  licenseType: "Licensed" | "Trainee";
  licenseNumber: string;
  issueDate: string;
  expiryDate: string;
  status: LicenseReviewStatus;
  /** Short-lived signed URL. */
  documentUrl: string | null;
  documentFileName: string | null;
  documentContentType: string | null;
  rejectionReason: string | null;
  correctionIssues: LicenseCorrectionIssue[];
  correctionNote: string | null;
  correctionRequestedAtUtc: string | null;
  resubmittedAtUtc: string | null;
  decidedAtUtc: string | null;
  /** Approved but the account isn't open yet: still has to confirm the email, then pay the fee. */
  onboardingStep: "VerifyEmail" | "PayFee" | null;
  photoUrl: string | null;
  /** Office / secretary numbers the lawyer added — admin-only, never shown to clients. */
  contactNumbers: { kind: "Office" | "Secretary" | "Mobile" | "Other"; contactName: string | null; phoneE164: string }[];
}

export async function getLawyerRegistrations(params: {
  status?: LicenseReviewStatus;
  search?: string;
  page: number;
  pageSize: number;
}): Promise<PagedResult<LawyerRegistrationSummaryDto>> {
  const { data } = await api.get<PagedResult<LawyerRegistrationSummaryDto>>("/api/v1/admin/lawyers", { params });
  return data;
}

export async function getLawyerRegistration(lawyerProfileId: string): Promise<LawyerRegistrationDetailDto> {
  const { data } = await api.get<LawyerRegistrationDetailDto>(`/api/v1/admin/lawyers/${lawyerProfileId}`);
  return data;
}

export async function requestLawyerChanges(lawyerProfileId: string, issues: LicenseCorrectionIssue[], note: string): Promise<void> {
  await api.post(`/api/v1/admin/lawyers/${lawyerProfileId}/request-changes`, { issues, note });
}

export async function verifyLawyer(lawyerProfileId: string): Promise<void> {
  await api.post(`/api/v1/admin/lawyers/${lawyerProfileId}/verify`);
}

export async function rejectLawyer(lawyerProfileId: string, reason: string): Promise<void> {
  await api.post(`/api/v1/admin/lawyers/${lawyerProfileId}/reject`, { reason });
}
