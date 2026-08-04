import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Star, Check, X } from "lucide-react";
import { Button, Card, Ltr, StatusTag } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDateTime } from "@law-portal/i18n";
import { acceptOffer, counterOffer, getOfferInbox, rejectOffer, type OfferSummaryDto } from "../lib/biddingApi";

const OFFER_STATUS_LABEL: Record<string, { ar: string; en: string }> = {
  Pending: { ar: "قيد التفاوض", en: "Negotiating" },
  Accepted: { ar: "مقبول", en: "Accepted" },
  Rejected: { ar: "مرفوض", en: "Rejected" },
  Withdrawn: { ar: "مسحوب", en: "Withdrawn" },
  Expired: { ar: "منتهي الصلاحية", en: "Expired" },
};

function OfferCard({ offer, requestId }: { offer: OfferSummaryDto; requestId: string }) {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const [counterAmount, setCounterAmount] = useState("");
  const [showCounter, setShowCounter] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function invalidate() {
    void queryClient.invalidateQueries({ queryKey: ["offerInbox", requestId] });
    void queryClient.invalidateQueries({ queryKey: ["requestDetail", requestId] });
  }

  const counterMutation = useMutation({
    mutationFn: () => counterOffer(offer.id, Number(counterAmount)),
    onSuccess: () => {
      setError(null);
      setShowCounter(false);
      setCounterAmount("");
      invalidate();
    },
    onError: () => setError(isAr ? "تعذّر إرسال المقابل." : "Could not send your counter-offer."),
  });

  const acceptMutation = useMutation({
    mutationFn: () => acceptOffer(offer.id),
    onSuccess: invalidate,
    onError: () => setError(isAr ? "تعذّر قبول هذا العرض." : "Could not accept this offer."),
  });

  const rejectMutation = useMutation({
    mutationFn: () => rejectOffer(offer.id),
    onSuccess: invalidate,
    onError: () => setError(isAr ? "تعذّر رفض هذا العرض." : "Could not reject this offer."),
  });

  const isPending = offer.status === "Pending";

  return (
    <Card>
      <div className="mb-3 flex items-start justify-between gap-3">
        <div>
          <p className="font-medium text-ink">{offer.lawyerFullName}</p>
          {offer.lawyerRatingCount > 0 && (
            <p className="mt-0.5 flex items-center gap-1 text-xs text-ink-faint">
              <Star className="h-3 w-3 fill-current text-warning" />
              <Ltr>{offer.lawyerAvgRating?.toFixed(1)}</Ltr>
              {" · "}
              {isAr
                ? `${offer.lawyerCompletedRequestCount} طلب مكتمل`
                : `${offer.lawyerCompletedRequestCount} completed`}
            </p>
          )}
        </div>
        <StatusTag
          status={offer.status}
          label={OFFER_STATUS_LABEL[offer.status] ? (isAr ? OFFER_STATUS_LABEL[offer.status].ar : OFFER_STATUS_LABEL[offer.status].en) : offer.status}
        />
      </div>

      <p className="mb-3 font-mono text-lg font-bold text-seal">
        <Ltr>{formatCurrency(offer.currentAmount)}</Ltr>
      </p>

      <div className="mb-3 flex flex-col gap-2 border-s-2 border-rule ps-3">
        {offer.revisions.map((r) => (
          <div key={r.id} className="text-xs">
            <span className="font-medium text-ink-soft">
              {r.proposedBy === "Lawyer" ? (isAr ? "المحامي" : "Lawyer") : isAr ? "أنت" : "You"}
            </span>
            {" — "}
            <Ltr className="font-mono">{formatCurrency(r.amount)}</Ltr>
            {r.message && <span className="text-ink-faint"> · {r.message}</span>}
            <span className="ms-1 text-ink-faint">
              <Ltr>{formatDateTime(r.createdAtUtc)}</Ltr>
            </span>
          </div>
        ))}
      </div>

      {isPending && (
        <div className="flex flex-col gap-2">
          {showCounter ? (
            <div className="flex items-center gap-2">
              <input
                type="number"
                value={counterAmount}
                onChange={(e) => setCounterAmount(e.target.value)}
                placeholder={isAr ? "المبلغ المقترح" : "Your counter amount"}
                className="w-32 rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
              />
              <Button
                variant="secondary"
                onClick={() => counterMutation.mutate()}
                disabled={!counterAmount || counterMutation.isPending}
              >
                {isAr ? "إرسال" : "Send"}
              </Button>
              <Button variant="ghost" onClick={() => setShowCounter(false)}>
                {isAr ? "إلغاء" : "Cancel"}
              </Button>
            </div>
          ) : (
            <div className="flex flex-wrap gap-2">
              <Button onClick={() => acceptMutation.mutate()} disabled={acceptMutation.isPending}>
                <Check className="h-4 w-4" />
                {isAr ? "قبول العرض" : "Accept offer"}
              </Button>
              <Button variant="secondary" onClick={() => setShowCounter(true)}>
                {isAr ? "تقديم مقابل" : "Counter"}
              </Button>
              <Button variant="danger" onClick={() => rejectMutation.mutate()} disabled={rejectMutation.isPending}>
                <X className="h-4 w-4" />
                {isAr ? "رفض" : "Reject"}
              </Button>
            </div>
          )}
        </div>
      )}
      {error && <p className="mt-2 text-sm text-rubric">{error}</p>}
    </Card>
  );
}

/** The client's offer inbox for one bidding request, mounted on OrderDetail while the request
 * is still Submitted (open to offers) — every invited lawyer's offer, its full negotiation
 * thread, and Counter/Accept/Reject actions. Accepting moves the request to Awarded, which
 * OrderDetail's own Pay-now panel then picks up unchanged. */
export function OfferInbox({ requestId }: { requestId: string }) {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";

  const query = useQuery({
    queryKey: ["offerInbox", requestId],
    queryFn: () => getOfferInbox(requestId),
    refetchInterval: 15000,
  });

  return (
    <div>
      <h2 className="mb-3 text-sm font-semibold text-ink-soft">
        {isAr ? `العروض الواردة (${query.data?.length ?? 0})` : `Offers received (${query.data?.length ?? 0})`}
      </h2>
      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}
      {query.data?.length === 0 && (
        <p className="text-sm text-ink-faint">
          {isAr ? "لم تصل عروض بعد — ستظهر هنا فور استلامها." : "No offers yet — they'll appear here as they arrive."}
        </p>
      )}
      <div className="flex flex-col gap-3">
        {query.data?.map((offer) => <OfferCard key={offer.id} offer={offer} requestId={requestId} />)}
      </div>
    </div>
  );
}
