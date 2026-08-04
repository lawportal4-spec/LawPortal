import { useState, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, ArrowRight, Lock, Paperclip, Send, XCircle } from "lucide-react";
import { Button, Card, Ltr } from "@law-portal/ui";
import { useTranslation, formatCurrency } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getBiddingRequestDetail, submitOffer, withdrawOffer } from "../lib/biddingApi";

const OFFER_STATUS_LABEL: Record<string, { ar: string; en: string }> = {
  Pending: { ar: "قيد التفاوض", en: "Negotiating" },
  Accepted: { ar: "مقبول", en: "Accepted" },
  Rejected: { ar: "مرفوض", en: "Rejected" },
  Withdrawn: { ar: "مسحوب", en: "Withdrawn" },
  Expired: { ar: "منتهي الصلاحية", en: "Expired" },
};

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex items-center justify-between border-b border-border py-3 text-sm last:border-0">
      <span className="text-ink-faint">{label}</span>
      <span className="font-medium text-ink">{children}</span>
    </div>
  );
}

/** One bidding request as seen by an invited lawyer. Title/details are visible to everyone
 * invited; attachments stay locked until this lawyer's offer is actually accepted — the
 * resolution to "970 lawyers can't see privileged documents" from a broadcast send. */
export default function BiddingDetail() {
  const { id } = useParams<{ id: string }>();
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["biddingDetail", id],
    queryFn: () => getBiddingRequestDetail(id!),
    enabled: !!id,
  });

  function invalidate() {
    void queryClient.invalidateQueries({ queryKey: ["biddingDetail", id] });
    void queryClient.invalidateQueries({ queryKey: ["biddingFeed"] });
  }

  const offerMutation = useMutation({
    mutationFn: () => submitOffer(id!, Number(amount), message || undefined),
    onSuccess: () => {
      setError(null);
      setAmount("");
      setMessage("");
      invalidate();
    },
    onError: () => setError(isAr ? "تعذّر إرسال العرض." : "Could not submit your offer."),
  });

  const withdrawMutation = useMutation({
    mutationFn: () => withdrawOffer(query.data!.myOfferId!),
    onSuccess: invalidate,
    onError: () => setError(isAr ? "تعذّر سحب العرض." : "Could not withdraw the offer."),
  });

  const canOffer = query.data && query.data.status === "Submitted" && query.data.myOfferStatus !== "Withdrawn";

  return (
    <AppShell>
      <Link to="/bidding" className="mb-6 inline-flex items-center gap-1.5 text-sm text-ink-faint hover:text-ink">
        {isAr ? <ArrowRight className="h-4 w-4" /> : <ArrowLeft className="h-4 w-4" />}
        {isAr ? "الرجوع إلى طلبات عروض الأسعار" : "Back to bidding feed"}
      </Link>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}
      {query.isError && <p className="text-sm text-rubric">{isAr ? "تعذّر تحميل الطلب." : "Could not load this request."}</p>}

      {query.data && (
        <div className="mx-auto max-w-2xl">
          <h1 className="mb-1 font-display text-2xl font-bold">
            {query.data.title ?? (isAr ? query.data.serviceNameAr : query.data.serviceNameEn)}
          </h1>
          <p className="mb-4 text-sm text-ink-faint">
            <Ltr className="font-mono">{query.data.number}</Ltr>
          </p>

          <Card className="mb-4">
            <Row label={isAr ? "الخدمة" : "Service"}>{isAr ? query.data.serviceNameAr : query.data.serviceNameEn}</Row>
            {query.data.specialtyNameAr && (
              <Row label={isAr ? "التخصص" : "Specialty"}>{isAr ? query.data.specialtyNameAr : query.data.specialtyNameEn}</Row>
            )}
          </Card>

          {query.data.description && (
            <Card className="mb-4">
              <h2 className="mb-2 text-sm font-semibold text-ink-soft">{isAr ? "التفاصيل" : "Details"}</h2>
              <p className="whitespace-pre-wrap text-sm text-ink">{query.data.description}</p>
            </Card>
          )}

          <Card className="mb-4">
            <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold text-ink-soft">
              {!query.data.attachmentsUnlocked && <Lock className="h-4 w-4" />}
              {isAr ? "المرفقات" : "Attachments"}
            </h2>
            {query.data.attachmentsUnlocked ? (
              query.data.attachmentFileNames.length > 0 ? (
                <div className="flex flex-col gap-2">
                  {query.data.attachmentFileNames.map((name) => (
                    <span key={name} className="flex items-center gap-2 text-sm text-ink">
                      <Paperclip className="h-4 w-4 text-ink-faint" />
                      {name}
                    </span>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-ink-faint">{isAr ? "لا توجد مرفقات." : "No attachments."}</p>
              )
            ) : (
              <p className="text-sm text-ink-faint">
                {isAr
                  ? "تُفتح المرفقات فقط للمحامي الذي يُقبل عرضه."
                  : "Attachments unlock only for the lawyer whose offer is accepted."}
              </p>
            )}
          </Card>

          <Card>
            <h2 className="mb-3 text-sm font-semibold text-ink-soft">{isAr ? "عرضك" : "Your offer"}</h2>

            {query.data.myOfferStatus && (
              <p className="mb-3 text-sm">
                <span className="text-ink-faint">{isAr ? "الحالة: " : "Status: "}</span>
                <span className="font-medium">
                  {OFFER_STATUS_LABEL[query.data.myOfferStatus]
                    ? isAr
                      ? OFFER_STATUS_LABEL[query.data.myOfferStatus].ar
                      : OFFER_STATUS_LABEL[query.data.myOfferStatus].en
                    : query.data.myOfferStatus}
                </span>
                {query.data.myLatestOfferAmount != null && (
                  <>
                    {" · "}
                    <Ltr className="font-mono">{formatCurrency(query.data.myLatestOfferAmount)}</Ltr>
                  </>
                )}
              </p>
            )}

            {canOffer ? (
              <div className="flex flex-col gap-3">
                <div className="flex items-center gap-2">
                  <input
                    type="number"
                    value={amount}
                    onChange={(e) => setAmount(e.target.value)}
                    placeholder={isAr ? "المبلغ المقترح (ر.س.)" : "Your price (SAR)"}
                    className="w-40 rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
                  />
                  <input
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                    placeholder={isAr ? "رسالة (اختياري)" : "Message (optional)"}
                    className="flex-1 rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
                  />
                </div>
                <div className="flex flex-wrap gap-3">
                  <Button onClick={() => offerMutation.mutate()} disabled={!amount || offerMutation.isPending}>
                    <Send className="h-4 w-4" />
                    {query.data.myOfferId
                      ? isAr
                        ? "إرسال سعر معدّل"
                        : "Send a revised price"
                      : isAr
                        ? "تقديم عرض"
                        : "Submit offer"}
                  </Button>
                  {query.data.myOfferId && query.data.myOfferStatus === "Pending" && (
                    <Button variant="danger" onClick={() => withdrawMutation.mutate()} disabled={withdrawMutation.isPending}>
                      <XCircle className="h-4 w-4" />
                      {isAr ? "سحب العرض" : "Withdraw offer"}
                    </Button>
                  )}
                </div>
              </div>
            ) : (
              <p className="text-sm text-ink-faint">
                {isAr ? "لم يعد هذا الطلب يقبل عروضًا." : "This request is no longer accepting offers."}
              </p>
            )}
            {error && <p className="mt-2 text-sm text-rubric">{error}</p>}
          </Card>
        </div>
      )}
    </AppShell>
  );
}
