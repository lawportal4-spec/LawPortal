import { api } from "./api";

export interface AuthResultDto {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  userId: string;
  userType: string;
}

export async function requestClientOtp(phoneE164: string): Promise<void> {
  await api.post("/api/v1/auth/client/otp/request", { phoneE164, recaptchaToken: "dev-placeholder" });
}

export async function verifyClientOtp(phoneE164: string, code: string): Promise<AuthResultDto> {
  const { data } = await api.post<AuthResultDto>("/api/v1/auth/client/otp/verify", { phoneE164, code });
  return data;
}

export interface MeDto {
  userId: string;
  userType: "Client" | "Lawyer" | "Admin";
  /** False until the client accepts the current "أتعهد…" pledge. */
  pledgeAccepted: boolean;
}

export async function getMe(): Promise<MeDto> {
  const { data } = await api.get<MeDto>("/api/v1/auth/me");
  return data;
}

export async function acceptPledge(): Promise<void> {
  await api.post("/api/v1/auth/me/pledge");
}
