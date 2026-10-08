import type { MyProfile, RegionOption } from "@law-portal/ui";
import { api } from "./api";

export async function getMyProfile(): Promise<MyProfile> {
  const { data } = await api.get<MyProfile>("/api/v1/account/profile");
  return data;
}

export async function saveMyProfile(input: { name: string; cityId: number | null }): Promise<void> {
  await api.put("/api/v1/account/profile", input);
}

export async function changeMyPassword(currentPassword: string, newPassword: string): Promise<void> {
  await api.post("/api/v1/account/password", { currentPassword, newPassword });
}

export async function getRegions(): Promise<RegionOption[]> {
  const { data } = await api.get<RegionOption[]>("/api/v1/catalog/regions");
  return data;
}

export async function getDeletionImpact(): Promise<import("@law-portal/ui").DeletionImpact> {
  const { data } = await api.get("/api/v1/account/deletion-impact");
  return data;
}

export async function deleteMyAccount(): Promise<void> {
  await api.delete("/api/v1/account");
}
