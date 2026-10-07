import type { ReactNode } from "react";
import { Card, Ltr } from "@law-portal/ui";
import { formatCurrency, useTranslation } from "@law-portal/i18n";

export function Money({ value, className = "" }: { value: number; className?: string }) {
  return <Ltr className={"font-mono tabular-nums " + className}>{formatCurrency(value)}</Ltr>;
}

export function Stat({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Card className="flex flex-col gap-1 py-3">
      <span className="text-xs text-ink-faint">{label}</span>
      <span className="text-xl font-semibold">{children}</span>
    </Card>
  );
}

export function Tabs<T extends string>({ tabs, value, onChange }: { tabs: { id: T; label: string }[]; value: T; onChange: (id: T) => void }) {
  return (
    <div className="mb-4 flex gap-1 overflow-x-auto border-b border-border" role="tablist">
      {tabs.map((tab) => (
        <button key={tab.id} type="button" role="tab" aria-selected={value === tab.id} onClick={() => onChange(tab.id)}
          className={"whitespace-nowrap border-b-2 px-4 py-2 text-sm " + (value === tab.id ? "border-seal text-seal-strong" : "border-transparent text-ink-faint hover:text-ink")}>
          {tab.label}
        </button>
      ))}
    </div>
  );
}

const REQUEST_PILL: Record<string, string> = {
  Draft: "bg-surface-raised text-ink-faint", Submitted: "bg-info-tint text-info", Awarded: "bg-info-tint text-info",
  Paid: "bg-seal-tint text-seal-strong", InProgress: "bg-seal-tint text-seal-strong", Completed: "bg-success-tint text-success",
  Cancelled: "bg-rubric-tint text-rubric", Refunded: "bg-rubric-tint text-rubric",
};

export function RequestStatus({ status }: { status: string }) {
  const { t } = useTranslation();
  return <span className={"inline-block rounded-full px-2.5 py-0.5 text-xs " + (REQUEST_PILL[status] ?? "bg-surface-raised text-ink-soft")}>{t(`directory.requestStatuses.${status}`, status)}</span>;
}

export const selectClass = "rounded-md border border-border bg-surface-raised px-3 py-2 text-sm";
