import { api } from "./api";

export type ConsultationType = "Instant" | "Written" | "Scheduled";
export type ConsultationDuration = 15 | 30 | 45;

export interface CreateConsultationDraftParams {
  specialtyId: number;
  lawyerProfileId: string;
  consultationType: ConsultationType;
  durationMinutes: ConsultationDuration | null;
}

export async function createConsultationDraft(params: CreateConsultationDraftParams): Promise<string> {
  const { data } = await api.post<string>("/api/v1/client/requests/consultations", params);
  return data;
}

export async function updateConsultationDetails(
  id: string,
  title: string,
  description: string,
  scheduledStartUtc: string | null,
): Promise<void> {
  await api.put(`/api/v1/client/requests/consultations/${id}/details`, {
    title,
    description,
    scheduledStartUtc,
    voiceNoteStorageKey: null,
    voiceNoteDurationSeconds: null,
  });
}
