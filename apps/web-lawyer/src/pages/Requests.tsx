import { useState } from "react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { ChevronLeft, ChevronRight, FileText } from "lucide-react";
import { Card, StatusPill, Ltr, type RequestStatus } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDate } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { listIncomingRequests } from "../lib/lawyerApi";

const STATUS_TO_PILL: Record<string, RequestStatus> = {
  Paid: "pendingPayment",
  InProgress: "inProgress",
  Completed: "completed",
  Refunded: "disputed",
};

const STATUS_FILTERS = [
  { value: "", ar: "كل الحالات", en: "All statuses" },
  { value: "Paid", ar: "بانتظار القبول", en: "Awaiting acceptance" },
  { value: "InProgress", ar: "قيد التنفيذ", en: "In progress" },
  { value: "Completed", ar: "مكتملة", en: "Completed" },
  { value: "Refunded", ar: "مُسترجَعة", en: "Refunded" },
] as const;

const STATUS_LABEL: Record<string, { ar: string; en: string }> = {
  Paid: { ar: "بانتظار القبول", en: "Awaiting acceptance" },
  InProgress: { ar: "قيد التنفيذ", en: "In progress" },
  Completed: { ar: "مكتمل", en: "Completed" },
  Refunded: { ar: "مُسترجَع", en: "Refunded" },
};

export default function Requests() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const [searchParams, setSearchParams] = useSearchParams();

  const status = searchParams.get("status") ?? "";
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const query = useQuery({
    queryKey: ["incomingRequests", { status, page, pageSize }],
    queryFn: () => listIncomingRequests(status || undefined, page, pageSize),
    placeholderData: keepPreviousData,
  });

  function handleStatusChange(value: string) {
    setSearchParams(value ? { status: value } : {});
    setPage(1);
  }

  return (
    <AppShell>
      <h1 className="mb-1 font-display text-2xl font-bold">{isAr ? "الطلبات الواردة" : "Incoming Requests"}</h1>
      <p className="mb-6 text-sm text-ink-faint">
        {isAr ? `${query.data?.totalCount ?? "…"} طلب` : `${query.data?.totalCount ?? "…"} requests`}
      </p>

      <div className="mb-6">
        <select
          className="rounded-md border border-border bg-surface-raised px-3 py-2.5 text-sm"
          value={status}
          onChange={(e) => handleStatusChange(e.target.value)}
        >
          {STATUS_FILTERS.map((s) => (
            <option key={s.value} value={s.value}>
              {isAr ? s.ar : s.en}
            </option>
          ))}
        </select>
      </div>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}
      {query.isError && <p className="text-sm text-rubric">{isAr ? "تعذّر تحميل الطلبات." : "Could not load requests."}</p>}

      {query.data && (
        <>
          <div className="flex flex-col gap-3">
            {query.data.items.map((r) => (
              <Link key={r.id} to={`/requests/${r.id}`}>
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
                        <Ltr>{formatDate(r.createdAtUtc)}</Ltr>
                        {" · "}
                        {r.clientName}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    {r.subtotal != null && (
                      <span className="font-mono text-sm text-ink-soft">
                        <Ltr>{formatCurrency(r.subtotal)}</Ltr>
                      </span>
                    )}
                    <StatusPill
                      status={STATUS_TO_PILL[r.status] ?? "draft"}
                      label={STATUS_LABEL[r.status] ? (isAr ? STATUS_LABEL[r.status].ar : STATUS_LABEL[r.status].en) : r.status}
                    />
                  </div>
                </Card>
              </Link>
            ))}
          </div>

          {query.data.items.length === 0 && (
            <p className="mt-8 text-center text-sm text-ink-faint">{isAr ? "لا توجد طلبات مطابقة." : "No matching requests."}</p>
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
