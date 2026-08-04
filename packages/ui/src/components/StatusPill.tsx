import { clsx } from "clsx";

export type RequestStatus =
  | "draft"
  | "pendingPayment"
  | "inProgress"
  | "completed"
  | "disputed"
  | "cancelled";

const STATUS_CLASSES: Record<RequestStatus, string> = {
  draft: "bg-paper text-ink-faint border border-border",
  pendingPayment: "bg-warning-tint text-warning",
  inProgress: "bg-seal-tint text-seal-strong",
  completed: "bg-seal text-seal-on",
  disputed: "bg-rubric-tint text-rubric",
  cancelled: "bg-transparent text-ink-faint border border-border line-through",
};

export function StatusPill({ status, label }: { status: RequestStatus; label: string }) {
  return (
    <span
      className={clsx(
        "inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium",
        "before:h-1.5 before:w-1.5 before:rounded-full before:bg-current",
        STATUS_CLASSES[status],
      )}
    >
      {label}
    </span>
  );
}
