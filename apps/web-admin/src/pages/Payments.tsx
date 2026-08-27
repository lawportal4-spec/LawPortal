import { useState } from "react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { Card, Ltr, StatusTag } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDateTime } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getPayments } from "../lib/financeApi";

const STATUSES = ["", "Initiated", "Paid", "Failed", "Refunded", "PartiallyRefunded"] as const;

export default function Payments() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 15;

  const query = useQuery({
    queryKey: ["adminPayments", { status, page }],
    queryFn: () => getPayments(status || undefined, page, pageSize),
    placeholderData: keepPreviousData,
  });

  return (
    <AppShell>
      <h1 className="mb-1 font-display text-2xl font-bold">{isAr ? "المدفوعات" : "Payments"}</h1>
      <p className="mb-6 text-sm text-ink-faint">
        {isAr ? `${query.data?.totalCount ?? "…"} عملية دفع` : `${query.data?.totalCount ?? "…"} payments`}
      </p>

      <div className="mb-6">
        <select
          className="rounded-md border border-border bg-surface-raised px-3 py-2.5 text-sm"
          value={status}
          onChange={(e) => {
            setStatus(e.target.value);
            setPage(1);
          }}
        >
          {STATUSES.map((s) => (
            <option key={s} value={s}>
              {s === "" ? (isAr ? "كل الحالات" : "All statuses") : s}
            </option>
          ))}
        </select>
      </div>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}

      <div className="flex flex-col gap-2">
        {query.data?.items.map((p) => (
          <Link key={p.id} to={`/payments/${p.id}`}>
            <Card className="flex items-center justify-between gap-4 transition-colors hover:border-seal">
              <div>
                <p className="font-mono text-sm font-medium text-ink">{p.number}</p>
                <p className="text-xs text-ink-faint">
                  {p.clientName?.startsWith("+") ? <Ltr className="font-mono">{p.clientName}</Ltr> : p.clientName ?? "—"} → {p.lawyerName ?? "—"} ·{" "}
                  <Ltr>{formatDateTime(p.createdAtUtc)}</Ltr>
                </p>
              </div>
              <div className="flex items-center gap-3">
                <span className="font-mono text-sm text-ink-soft">
                  <Ltr>{formatCurrency(p.total)}</Ltr>
                </span>
                <StatusTag status={p.status} />
              </div>
            </Card>
          </Link>
        ))}
      </div>

      {query.data && (
        <div className="mt-8 flex items-center justify-center gap-4">
          <button
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-2 text-sm disabled:opacity-40"
          >
            <ChevronLeft className="h-4 w-4 rtl:hidden" />
            <ChevronRight className="h-4 w-4 ltr:hidden" />
            {isAr ? "السابق" : "Previous"}
          </button>
          <span className="font-mono text-sm text-ink-soft">
            {page} / {query.data.totalPages || 1}
          </span>
          <button
            disabled={page >= query.data.totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-2 text-sm disabled:opacity-40"
          >
            {isAr ? "التالي" : "Next"}
            <ChevronRight className="h-4 w-4 rtl:hidden" />
            <ChevronLeft className="h-4 w-4 ltr:hidden" />
          </button>
        </div>
      )}
    </AppShell>
  );
}
