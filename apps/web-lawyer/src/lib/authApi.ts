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

export interface LawyerMeDto {
  fullName: string;
  isApproved: boolean;
}

export async function getLawyerMe(): Promise<LawyerMeDto> {
  const { data } = await api.get<LawyerMeDto>("/api/v1/lawyer/me");
  return data;
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
