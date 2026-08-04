import * as signalR from "@microsoft/signalr";
import { api } from "./api";

export interface MessageDto {
  id: string;
  threadId: string;
  senderUserId: string;
  body: string;
  sentAtUtc: string;
}

export interface ThreadSummaryDto {
  id: string;
  serviceRequestId: string;
  requestNumber: string;
  otherPartyName: string;
  lastMessageBody: string | null;
  lastMessageAtUtc: string | null;
  unreadCount: number;
}

export interface ThreadDetailDto {
  id: string;
  serviceRequestId: string;
  requestNumber: string;
  otherPartyUserId: string;
  otherPartyName: string;
  isOtherPartyOnline: boolean;
  isBlockedByMe: boolean;
  hasBlockedMe: boolean;
}

export async function listMyThreads(): Promise<ThreadSummaryDto[]> {
  const { data } = await api.get<ThreadSummaryDto[]>("/api/v1/chat/threads");
  return data;
}

export async function getThreadDetail(threadId: string): Promise<ThreadDetailDto> {
  const { data } = await api.get<ThreadDetailDto>(`/api/v1/chat/threads/${threadId}`);
  return data;
}

export async function getMessages(threadId: string, afterUtc?: string): Promise<MessageDto[]> {
  const { data } = await api.get<MessageDto[]>(`/api/v1/chat/threads/${threadId}/messages`, {
    params: afterUtc ? { after: afterUtc } : undefined,
  });
  return data;
}

export async function sendMessage(threadId: string, body: string): Promise<MessageDto> {
  const { data } = await api.post<MessageDto>(`/api/v1/chat/threads/${threadId}/messages`, { body });
  return data;
}

export async function markThreadRead(threadId: string): Promise<void> {
  await api.post(`/api/v1/chat/threads/${threadId}/read`);
}

export async function blockThreadParticipant(threadId: string): Promise<void> {
  await api.post(`/api/v1/chat/threads/${threadId}/block`);
}

export async function unblockThreadParticipant(threadId: string): Promise<void> {
  await api.delete(`/api/v1/chat/threads/${threadId}/block`);
}

export async function reportThreadParticipant(threadId: string, reason: string, details?: string): Promise<void> {
  await api.post(`/api/v1/chat/threads/${threadId}/report`, { reason, details });
}

const HUB_URL = `${import.meta.env.VITE_API_URL ?? "http://localhost:5280"}/hubs/chat`;

export function connectChatHub(accessToken: string): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL, { accessTokenFactory: () => accessToken })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build();
}

/** The JWT's own claims are the only source for "which messages are mine" client-side — no
 * separate /me round trip needed just to compare a sender id. */
export function getUserIdFromToken(accessToken: string): string | null {
  try {
    const payload = JSON.parse(atob(accessToken.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")));
    return typeof payload.sub === "string" ? payload.sub : null;
  } catch {
    return null;
  }
}
