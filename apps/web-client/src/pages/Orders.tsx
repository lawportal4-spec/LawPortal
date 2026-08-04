import { useState } from "react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { ChevronLeft, ChevronRight, FileText } from "lucide-react";
import { Card, StatusPill, Ltr, type RequestStatus } from "@law-portal/ui";
import { useTranslation, formatCurrency as fmtCurrency, formatDate as fmtDate } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { listMyRequests } from "../lib/requestsApi";

const STATUS_TO_PILL: Record<string, RequestStatus> = {
  Draft: "draft",
  Submitted: "inProgress",
  // Bidding-only — the client accepted an offer but hasn't paid yet.
  Awarded: "pendingPayment",
  Paid: "completed",
  InProgress: "inProgress",
  Completed: "completed",
  Cancelled: "cancelled",
  Refunded: "disputed",
};

const STATUS_FILTERS = [
  { value: "", ar: "كل الحالات", en: "All statuses" },
  { value: "Draft", ar: "مسودة", en: "Draft" },
  { value: "Submitted", ar: "مُقدَّم", en: "Submitted" },
  { value: "Awarded", ar: "تم الترسية", en: "Awarded" },
  { value: "Paid", ar: "مدفوع", en: "Paid" },
  { value: "InProgress", ar: "قيد التنفيذ", en: "In progress" },
  { value: "Completed", ar: "مكتمل", en: "Completed" },
  { value: "Cancelled", ar: "ملغى", en: "Cancelled" },
  { value: "Refunded", ar: "مُسترجَع", en: "Refunded" },
] as const;

const KIND_FILTERS = [
  { value: "", ar: "كل الأنواع", en: "All kinds" },
  { value: "Consultation", ar: "استشارة", en: "Consultation" },
  { value: "Catalog", ar: "خدمة توثيق", en: "Catalog service" },
  { value: "Bidding", ar: "عرض أسعار", en: "Bidding" },
] as const;

export default function Orders() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";

  const [status, setStatus] = useState("");
  const [kind, setKind] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const query = useQuery({
    queryKey: ["myRequests", { status, kind, page, pageSize }],
    queryFn: () =>
      listMyRequests({
        status: status || undefined,
        kind: kind || undefined,
        page,
        pageSize,
      }),
    placeholderData: keepPreviousData,
  });

  function resetToFirstPage<T>(setter: (v: T) => void) {
    return (value: T) => {
      setter(value);
      setPage(1);
    };
  }

  return (
    <AppShell>
      <h1 className="mb-1 font-display text-2xl font-bold">{isAr ? "طلباتي" : "My Orders"}</h1>
      <p className="mb-6 text-sm text-ink-faint">
        {isAr
          ? `${query.data?.totalCount ?? "…"} طلب`
          : `${query.data?.totalCount ?? "…"} requests`}
      </p>

      <div className="mb-6 flex flex-wrap items-center gap-3">
        <select
          className="rounded-md border border-border bg-surface-raised px-3 py-2.5 text-sm"
          value={status}
          onChange={(e) => resetToFirstPage(setStatus)(e.target.value)}
        >
          {STATUS_FILTERS.map((s) => (
            <option key={s.value} value={s.value}>
              {isAr ? s.ar : s.en}
            </option>
          ))}
        </select>

        <select
          className="rounded-md border border-border bg-surface-raised px-3 py-2.5 text-sm"
          value={kind}
          onChange={(e) => resetToFirstPage(setKind)(e.target.value)}
        >
          {KIND_FILTERS.map((k) => (
            <option key={k.value} value={k.value}>
              {isAr ? k.ar : k.en}
            </option>
          ))}
        </select>
      </div>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}
      {query.isError && (
        <p className="text-sm text-rubric">{isAr ? "تعذّر تحميل الطلبات." : "Could not load your orders."}</p>
      )}

      {query.data && (
        <>
          <div className="flex flex-col gap-3">
            {query.data.items.map((r) => (
              <Link key={r.id} to={`/orders/${r.id}`}>
                <Card className="flex items-center justify-between gap-4 transition-colors hover:border-seal">
                  <div className="flex items-center gap-3">
                    <span className="flex h-10 w-10 items-center justify-center rounded-full bg-paper text-seal">
                      <FileText className="h-5 w-5" />
                    </span>
                    <div>
                      <p className="font-medium text-ink">{isAr ? r.serviceNameAr : r.serviceNameEn}</p>
                      <p className="text-xs text-ink-faint">
                        <Ltr className="font-mono">{r.number}</Ltr>
                        {" · "}
                        <Ltr>{fmtDate(r.createdAtUtc)}</Ltr>
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    {r.subtotal != null && (
                      <span className="font-mono text-sm text-ink-soft">
                        <Ltr>{fmtCurrency(r.subtotal)}</Ltr>
                      </span>
                    )}
                    <StatusPill
                      status={STATUS_TO_PILL[r.status]}
                      label={
                        STATUS_FILTERS.find((s) => s.value === r.status)
                          ? isAr
                            ? STATUS_FILTERS.find((s) => s.value === r.status)!.ar
                            : STATUS_FILTERS.find((s) => s.value === r.status)!.en
                          : r.status
                      }
                    />
                  </div>
                </Card>
              </Link>
            ))}
          </div>

          {query.data.items.length === 0 && (
            <p className="mt-8 text-center text-sm text-ink-faint">
              {isAr ? "لا توجد طلبات مطابقة." : "No matching orders."}
            </p>
          )}

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
        </>
      )}
    </AppShell>
  );
}
