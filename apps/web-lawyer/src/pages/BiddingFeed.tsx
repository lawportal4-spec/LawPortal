import { useState } from "react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { ChevronLeft, ChevronRight, Gavel } from "lucide-react";
import { Card, Ltr } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDate } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getBiddingFeed } from "../lib/biddingApi";

const OFFER_STATUS_LABEL: Record<string, { ar: string; en: string }> = {
  Pending: { ar: "قيد التفاوض", en: "Negotiating" },
  Accepted: { ar: "مقبول", en: "Accepted" },
  Rejected: { ar: "مرفوض", en: "Rejected" },
  Withdrawn: { ar: "مسحوب", en: "Withdrawn" },
  Expired: { ar: "منتهي الصلاحية", en: "Expired" },
};

/** The lawyer's bidding feed — every still-open request this lawyer was invited to bid on,
 * whether hand-picked by the client or matched by a broadcast fan-out. */
export default function BiddingFeed() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const query = useQuery({
    queryKey: ["biddingFeed", { page, pageSize }],
    queryFn: () => getBiddingFeed(page, pageSize),
    placeholderData: keepPreviousData,
  });

  return (
    <AppShell>
      <h1 className="mb-1 font-display text-2xl font-bold">{isAr ? "طلبات عروض الأسعار" : "Bidding Feed"}</h1>
      <p className="mb-6 text-sm text-ink-faint">
        {isAr ? `${query.data?.totalCount ?? "…"} طلب مفتوح` : `${query.data?.totalCount ?? "…"} open requests`}
      </p>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}
      {query.isError && <p className="text-sm text-rubric">{isAr ? "تعذّر تحميل الطلبات." : "Could not load requests."}</p>}

      {query.data && (
        <>
          <div className="flex flex-col gap-3">
            {query.data.items.map((r) => (
              <Link key={r.id} to={`/bidding/${r.id}`}>
                <Card className="flex items-center justify-between gap-4 transition-colors hover:border-seal">
                  <div className="flex items-center gap-3">
                    <span className="flex h-10 w-10 items-center justify-center rounded-full bg-paper text-seal">
                      <Gavel className="h-5 w-5" />
                    </span>
                    <div>
                      <p className="font-medium text-ink">{r.title ?? (isAr ? r.serviceNameAr : r.serviceNameEn)}</p>
                      <p className="text-xs text-ink-faint">
                        <Ltr className="font-mono">{r.number}</Ltr>
                        {" · "}
                        {r.specialtyNameAr && (isAr ? r.specialtyNameAr : r.specialtyNameEn)}
                        {" · "}
                        <Ltr>{formatDate(r.createdAtUtc)}</Ltr>
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    {r.myLatestOfferAmount != null && (
                      <span className="font-mono text-sm text-ink-soft">
                        <Ltr>{formatCurrency(r.myLatestOfferAmount)}</Ltr>
                      </span>
                    )}
                    <span
                      className={
                        "rounded-full px-3 py-1 text-xs font-medium " +
                        (r.myOfferStatus ? "bg-seal-tint text-seal-strong" : "border border-border text-ink-faint")
                      }
                    >
                      {r.myOfferStatus
                        ? OFFER_STATUS_LABEL[r.myOfferStatus]
                          ? isAr
                            ? OFFER_STATUS_LABEL[r.myOfferStatus].ar
                            : OFFER_STATUS_LABEL[r.myOfferStatus].en
                          : r.myOfferStatus
                        : isAr
                          ? "لم تُقدّم عرضًا بعد"
                          : "No offer yet"}
                    </span>
                  </div>
                </Card>
              </Link>
            ))}
          </div>

          {query.data.items.length === 0 && (
            <p className="mt-8 text-center text-sm text-ink-faint">
              {isAr ? "لا توجد طلبات مفتوحة حاليًا." : "No open bidding requests right now."}
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
