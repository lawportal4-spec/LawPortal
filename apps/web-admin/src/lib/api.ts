import axios from "axios";

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "http://localhost:5280",
});

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** "2026-10-07" (a date input) → start / end of that local day, as ISO. */
export const dayStart = (d: string) => (d ? new Date(`${d}T00:00:00`).toISOString() : undefined);
export const dayEnd = (d: string) => (d ? new Date(`${d}T23:59:59`).toISOString() : undefined);
