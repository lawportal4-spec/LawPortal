import { useState } from "react";
import { useMutation, useQuery, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight, CheckCircle2, Gavel, MessageCircle } from "lucide-react";
import { Link } from "react-router-dom";
import { Button, Card, StatusPill, Ltr, type RequestStatus } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDate } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getAwardedBiddingRequests } from "../lib/biddingApi";
import { completeRequest } from "../lib/lawyerApi";

const STATUS_TO_PILL: Record<string, RequestStatus> = {
  Paid: "pendingPayment",
  Completed: "completed",
  Refunded: "disputed",
};

const STATUS_LABEL: Record<string, { ar: string; en: string }> = {
  Paid: { ar: "قيد التنفيذ", en: "In progress" },
  Completed: { ar: "مكتمل", en: "Completed" },
  Refunded: { ar: "مُسترجَع", en: "Refunded" },
};

/** Bidding requests this lawyer actually won — an accepted offer, now Paid or Completed. There's
 * no separate "accept" step here (unlike consultations): having an offer accepted already
 * committed the lawyer, so a Paid bidding request goes straight to Completed via the same shared
 * complete action every request shape uses. */
export default function BiddingAwarded() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const query = useQuery({
    queryKey: ["biddingAwarded", { page, pageSize }],
    queryFn: () => getAwardedBiddingRequests(undefined, page, pageSize),
    placeholderData: keepPreviousData,
  });

  const completeMutation = useMutation({
    mutationFn: (id: string) => completeRequest(id),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["biddingAwarded"] }),
  });

  return (
    <AppShell>
      <h1 className="mb-1 font-display text-2xl font-bold">{isAr ? "الطلبات الفائزة" : "Awarded Requests"}</h1>
      <p className="mb-6 text-sm text-ink-faint">
        {isAr ? `${query.data?.totalCount ?? "…"} طلب` : `${query.data?.totalCount ?? "…"} requests`}
      </p>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}

      {query.data && (
        <>
          <div className="flex flex-col gap-3">
            {query.data.items.map((r) => (
              <Card key={r.id} className="flex items-center justify-between gap-4">
                <div className="flex items-center gap-3">
                  <span className="flex h-10 w-10 items-center justify-center rounded-full bg-paper text-seal">
                    <Gavel className="h-5 w-5" />
                  </span>
                  <div>
                    <p className="font-medium text-ink">{r.title ?? (isAr ? r.serviceNameAr : r.serviceNameEn)}</p>
                    <p className="text-xs text-ink-faint">
                      <Ltr className="font-mono">{r.number}</Ltr>
                      {" · "}
                      {r.clientName}
                      {" · "}
                      <Ltr>{formatDate(r.createdAtUtc)}</Ltr>
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  {r.subtotal != null && (
                    <span className="font-mono text-sm text-ink-soft">
                      <Ltr>{formatCurrency(r.subtotal)}</Ltr>
                    </span>
                  )}
                  <StatusPill
                    status={STATUS_TO_PILL[r.status] ?? "draft"}
                    label={STATUS_LABEL[r.status] ? (isAr ? STATUS_LABEL[r.status].ar : STATUS_LABEL[r.status].en) : r.status}
                  />
                  {r.status === "Paid" && (
                    <Button onClick={() => completeMutation.mutate(r.id)} disabled={completeMutation.isPending}>
                      <CheckCircle2 className="h-4 w-4" />
                      {isAr ? "تحديد كمكتمل" : "Mark complete"}
                    </Button>
                  )}
                  <Link to={`/chat/${r.id}`}>
                    <Button variant="ghost">
                      <MessageCircle className="h-4 w-4" />
                    </Button>
                  </Link>
                </div>
              </Card>
            ))}
          </div>

          {query.data.items.length === 0 && (
            <p className="mt-8 text-center text-sm text-ink-faint">
              {isAr ? "لم تفز بأي طلبات عروض أسعار بعد." : "You haven't won any bidding requests yet."}
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
