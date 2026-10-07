import { api } from "./api";

export type ContactNumberKind = "Office" | "Secretary" | "Mobile" | "Other";
export const CONTACT_KINDS: ContactNumberKind[] = ["Office", "Secretary", "Mobile", "Other"];

export interface ContactNumberDto {
  kind: ContactNumberKind;
  contactName: string | null;
  phoneE164: string;
}

export interface LawyerAccountDto {
  fullName: string;
  photoUrl: string | null;
  phoneE164: string | null;
  email: string | null;
  /** A new address waiting for its confirmation link. */
  pendingEmail: string | null;
  regionId: number | null;
  cityId: number | null;
  contactNumbers: ContactNumberDto[];
}

export async function getAccount(): Promise<LawyerAccountDto> {
  const { data } = await api.get<LawyerAccountDto>("/api/v1/lawyer/account");
  return data;
}

export async function uploadPhoto(file: File): Promise<void> {
  const form = new FormData();
  form.append("photo", file);
  await api.post("/api/v1/lawyer/account/photo", form);
}

export async function removePhoto(): Promise<void> {
  await api.delete("/api/v1/lawyer/account/photo");
}

export async function requestPhoneChange(phoneE164: string): Promise<void> {
  await api.post("/api/v1/lawyer/account/phone/request", { phoneE164 });
}

export async function confirmPhoneChange(phoneE164: string, code: string): Promise<void> {
  await api.post("/api/v1/lawyer/account/phone/confirm", { phoneE164, code });
}

export async function requestEmailChange(email: string): Promise<void> {
  await api.post("/api/v1/lawyer/account/email/request", { email });
}

export async function updateLocation(regionId: number, cityId: number): Promise<void> {
  await api.put("/api/v1/lawyer/account/location", { regionId, cityId });
}

export async function saveContactNumbers(numbers: ContactNumberDto[]): Promise<void> {
  await api.put("/api/v1/lawyer/account/contact-numbers", { numbers });
}

/** "0114567890" / "0551234567" / "920012345" → "+966…"; null when it isn't a Saudi number. */
export function toSaudiAnyE164(local: string): string | null {
  const digits = local.replace(/\D/g, "").replace(/^966/, "").replace(/^0/, "");
  return /^\d{8,10}$/.test(digits) ? `+966${digits}` : null;
}

/** "+966114567890" → "0114567890" (800/920 numbers keep no leading zero). */
export function toLocalNumber(e164: string): string {
  const rest = e164.replace(/^\+966/, "");
  return /^[15]/.test(rest) ? `0${rest}` : rest;
}

export async function changeMyPassword(currentPassword: string, newPassword: string): Promise<void> {
  await api.post("/api/v1/account/password", { currentPassword, newPassword });
}

export async function getDeletionImpact(): Promise<import("@law-portal/ui").DeletionImpact> {
  const { data } = await api.get("/api/v1/account/deletion-impact");
  return data;
}

export async function deleteMyAccount(): Promise<void> {
  await api.delete("/api/v1/account");
}
