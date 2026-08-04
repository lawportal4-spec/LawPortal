import { useQuery } from "@tanstack/react-query";
import { Star } from "lucide-react";
import { Card, Ltr, StatusPill, type RequestStatus } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDate } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getMyEarnings, getMyReviews } from "../lib/lawyerApi";

const STATUS_TO_PILL: Record<string, RequestStatus> = { Held: "pendingPayment", Released: "completed" };
const STATUS_LABEL: Record<string, { ar: string; en: string }> = {
  Held: { ar: "محتجز", en: "Held" },
  Released: { ar: "مُحرَّر", en: "Released" },
};

export default function Earnings() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";

  const earningsQuery = useQuery({ queryKey: ["myEarnings"], queryFn: getMyEarnings });
  const reviewsQuery = useQuery({ queryKey: ["myReviews"], queryFn: getMyReviews });

  return (
    <AppShell>
      <h1 className="mb-6 font-display text-2xl font-bold">{isAr ? "الأرباح والتقييمات" : "Earnings & Reviews"}</h1>

      <div className="mx-auto flex max-w-2xl flex-col gap-6">
        <Card>
          <h2 className="mb-4 text-sm font-semibold text-ink-soft">{isAr ? "سجل الأرباح" : "Earnings history"}</h2>
          <div className="flex flex-col gap-3">
            {earningsQuery.data?.map((p) => (
              <div key={p.id} className="flex items-center justify-between border-b border-border pb-3 text-sm last:border-0">
                <div>
                  <p className="font-mono text-ink">
                    <Ltr>{p.requestNumber}</Ltr>
                  </p>
                  <p className="text-xs text-ink-faint">
                    <Ltr>{formatDate(p.createdAtUtc)}</Ltr>
                  </p>
                </div>
                <div className="flex items-center gap-3">
                  <span className="font-mono text-ink">
                    <Ltr>{formatCurrency(p.amount)}</Ltr>
                  </span>
                  <StatusPill
                    status={STATUS_TO_PILL[p.status] ?? "draft"}
                    label={STATUS_LABEL[p.status] ? (isAr ? STATUS_LABEL[p.status].ar : STATUS_LABEL[p.status].en) : p.status}
                  />
                </div>
              </div>
            ))}
            {earningsQuery.data?.length === 0 && (
              <p className="text-sm text-ink-faint">{isAr ? "لا توجد أرباح بعد." : "No earnings yet."}</p>
            )}
          </div>
        </Card>

        <Card>
          <h2 className="mb-4 text-sm font-semibold text-ink-soft">{isAr ? "التقييمات المستلمة" : "Reviews received"}</h2>
          <div className="flex flex-col gap-3">
            {reviewsQuery.data?.map((r) => (
              <div key={r.id} className="border-b border-border pb-3 text-sm last:border-0">
                <div className="mb-1 flex items-center justify-between">
                  <div className="flex items-center gap-1">
                    {Array.from({ length: 5 }).map((_, i) => (
                      <Star key={i} className={`h-4 w-4 ${i < r.rating ? "fill-seal text-seal" : "text-border"}`} />
                    ))}
                  </div>
                  <span className="font-mono text-xs text-ink-faint">
                    <Ltr>{formatDate(r.createdAtUtc)}</Ltr>
                  </span>
                </div>
                {r.comment && <p className="text-ink">{r.comment}</p>}
                <p className="mt-1 font-mono text-xs text-ink-faint">
                  <Ltr>{r.requestNumber}</Ltr>
                </p>
              </div>
            ))}
            {reviewsQuery.data?.length === 0 && (
              <p className="text-sm text-ink-faint">{isAr ? "لا توجد تقييمات بعد." : "No reviews yet."}</p>
            )}
          </div>
        </Card>
      </div>
    </AppShell>
  );
}
