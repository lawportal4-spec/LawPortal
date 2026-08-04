import { api, type PagedResult } from "./api";

export interface AuditLogDto {
  id: string;
  actorUserId: string | null;
  actorRole: string | null;
  action: string;
  entityType: string | null;
  entityId: string | null;
  details: string | null;
  ip: string | null;
  occurredAtUtc: string;
}

export async function getAuditLogs(params: { action?: string; from?: string; to?: string; page?: number; pageSize?: number }): Promise<PagedResult<AuditLogDto>> {
  const { data } = await api.get<PagedResult<AuditLogDto>>("/api/v1/admin/audit-logs", { params });
  return data;
}
