import { clsx } from "clsx";

type Tone = "positive" | "negative" | "warning" | "neutral";

const POSITIVE = new Set(["Paid", "Verified", "Approved", "Completed", "Active", "Accepted"]);
const NEGATIVE = new Set(["Failed", "Rejected", "Refunded", "Disputed", "Cancelled", "Withdrawn", "Expired"]);
const WARNING = new Set(["Initiated", "Pending", "PartiallyRefunded", "Draft"]);

function toneFor(status: string): Tone {
  if (POSITIVE.has(status)) return "positive";
  if (NEGATIVE.has(status)) return "negative";
  if (WARNING.has(status)) return "warning";
  return "neutral";
}

const TONE_CLASSES: Record<Tone, string> = {
  positive: "bg-seal-tint text-seal-strong",
  negative: "bg-rubric-tint text-rubric",
  warning: "bg-warning-tint text-warning",
  neutral: "bg-paper text-ink-soft",
};

export interface StatusTagProps {
  status: string;
  className?: string;
}

/**
 * Colors an arbitrary backend status string by keyword — for enums (payment status,
 * verification status) that don't map onto the fixed RequestStatus set StatusPill covers.
 * An unrecognized status falls back to the same neutral tag it would render as today.
 */
export function StatusTag({ status, className }: StatusTagProps) {
  return (
    <span className={clsx("rounded-full px-3 py-1 text-xs font-medium", TONE_CLASSES[toneFor(status)], className)}>
      {status}
    </span>
  );
}
