import { api } from "./api";

export interface AuthResultDto {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  userId: string;
  userType: string;
}

export async function lawyerLogin(phoneE164: string, password: string): Promise<AuthResultDto> {
  const { data } = await api.post<AuthResultDto>("/api/v1/auth/lawyer/login", {
    phoneE164,
    password,
    recaptchaToken: "dev-placeholder",
  });
  return data;
}

export type LicenseType = "Licensed" | "Trainee";

export interface RegisterLawyerBody {
  fullName: string;
  phoneE164: string;
  email: string;
  password: string;
  regionId: number;
  cityId: number;
  licenseType: LicenseType;
  licenseNumber: string;
  /** Gregorian ISO date (yyyy-mm-dd) — the wizard converts from the Hijri the lawyer typed. */
  issueDate: string;
  expiryDate: string;
  countryCode: "SA";
  acceptedTerms: boolean;
  licenseDocument: File;
}

/** Creates the (unactivated) account and texts an activation code to `phoneE164`. */
export async function registerLawyer(body: RegisterLawyerBody): Promise<void> {
  const form = new FormData();
  for (const [key, value] of Object.entries(body)) {
    form.append(key, value instanceof File ? value : String(value));
  }
  form.append("recaptchaToken", "dev-placeholder");
  await api.post("/api/v1/auth/lawyer/register", form);
}

export async function verifyLawyerRegistration(phoneE164: string, code: string): Promise<void> {
  await api.post("/api/v1/auth/lawyer/register/verify", { phoneE164, code });
}

export async function resendLawyerRegistrationCode(phoneE164: string): Promise<void> {
  await api.post("/api/v1/auth/lawyer/register/resend", { phoneE164 });
}

/** Always succeeds — whether the number is registered is deliberately not revealed. */
export async function requestPasswordReset(phoneE164: string): Promise<void> {
  await api.post("/api/v1/auth/lawyer/password/forgot", { phoneE164 });
}

/** Confirms the code before the new-password step; the reset call sends it again. */
export async function verifyResetCode(phoneE164: string, code: string): Promise<void> {
  await api.post("/api/v1/auth/lawyer/password/verify-code", { phoneE164, code });
}

export async function resetPassword(phoneE164: string, code: string, newPassword: string): Promise<void> {
  await api.post("/api/v1/auth/lawyer/password/reset", { phoneE164, code, newPassword });
}

export type ReviewStatus = "PendingReview" | "ChangesRequested" | "Approved" | "Rejected" | "Expired";

export interface LawyerMeDto {
  fullName: string;
  isApproved: boolean;
  reviewStatus: ReviewStatus;
  rejectionReason: string | null;
  /** Keys under lawyerReview.issues — what the admin asked to fix. */
  correctionIssues: string[];
  correctionNote: string | null;
  licenseType: LicenseType;
  licenseNumber: string;
  /** Gregorian ISO dates; the correction form shows them in Hijri. */
  issueDate: string;
  expiryDate: string;
  documentFileName: string | null;
}

export async function getLawyerMe(): Promise<LawyerMeDto> {
  const { data } = await api.get<LawyerMeDto>("/api/v1/lawyer/me");
  return data;
}

/** Answers "returned for changes": the licence step again; the file only if replacing it. */
export async function resubmitLicense(body: {
  licenseType: LicenseType;
  licenseNumber: string;
  issueDate: string;
  expiryDate: string;
  licenseDocument: File | null;
}): Promise<void> {
  const form = new FormData();
  form.append("licenseType", body.licenseType);
  form.append("licenseNumber", body.licenseNumber);
  form.append("issueDate", body.issueDate);
  form.append("expiryDate", body.expiryDate);
  if (body.licenseDocument) form.append("licenseDocument", body.licenseDocument);
  await api.post("/api/v1/lawyer/license/resubmit", form);
}

/** "5XXXXXXXX" (as typed after the fixed +966 prefix) → "+9665XXXXXXXX", or null. */
export function toSaudiE164(localDigits: string): string | null {
  const digits = localDigits.replace(/\D/g, "").replace(/^0/, "");
  return /^5\d{8}$/.test(digits) ? `+966${digits}` : null;
}

export interface CityDto {
  id: number;
  nameAr: string;
  nameEn: string;
}

export interface RegionDto {
  id: number;
  nameAr: string;
  nameEn: string;
  cities: CityDto[];
}

export async function getRegions(): Promise<RegionDto[]> {
  const { data } = await api.get<RegionDto[]>("/api/v1/catalog/regions");
  return data;
}
