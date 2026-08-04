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
