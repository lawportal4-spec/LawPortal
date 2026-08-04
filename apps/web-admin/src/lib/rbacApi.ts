import { api } from "./api";

export interface AdminUserDto {
  userId: string;
  displayName: string;
  email: string;
  roles: string[];
  createdAtUtc: string;
  lastLoginAtUtc: string | null;
}

export async function getAdminUsers(): Promise<AdminUserDto[]> {
  const { data } = await api.get<AdminUserDto[]>("/api/v1/admin/users");
  return data;
}

export async function inviteAdmin(displayName: string, email: string, password: string): Promise<string> {
  const { data } = await api.post<string>("/api/v1/admin/users/invite", { displayName, email, password });
  return data;
}

export interface RoleDto {
  id: number;
  name: string;
  permissions: string[];
}

export async function getRoles(): Promise<RoleDto[]> {
  const { data } = await api.get<RoleDto[]>("/api/v1/admin/roles");
  return data;
}
