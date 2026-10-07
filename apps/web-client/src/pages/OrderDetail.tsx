import { useEffect, useState, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { ArrowLeft, ArrowRight, MessageCircle, Paperclip } from "lucide-react";
import { Button, Card, StatusPill, Ltr, type RequestStatus } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDateTime } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getRequestDetail } from "../lib/requestsApi";
import { checkout, getInvoice } from "../lib/paymentsApi";
import { OfferInbox } from "../components/OfferInbox";
import { CheckoutPanel, type CheckoutMethod } from "../components/CheckoutPanel";
import { isAxiosError } from "axios";

/** How long to keep polling for the webhook after the gateway hands the payer back, before
 * falling back to the manual button. Long enough for a slow webhook, short enough that a tab left
 * open on a failed payment stops hitting the API. */
const GATEWAY_POLL_TIMEOUT_MS = 90_000;

const STATUS_TO_PILL: Record<string, RequestStatus> = {
  Draft: "draft",
  Submitted: "inProgress",
  Awarded: "pendingPayment",
  Paid: "completed",
  InProgress: "inProgress",
  Completed: "completed",
  Cancelled: "cancelled",
  Refunded: "disputed",
};

const STATUS_LABEL: Record<string, { ar: string; en: string }> = {
  Draft: { ar: "مسودة", en: "Draft" },
  Submitted: { ar: "مُقدَّم", en: "Submitted" },
  Awarded: { ar: "تم الترسية", en: "Awarded" },
  Paid: { ar: "مدفوع", en: "Paid" },
  InProgress: { ar: "قيد التنفيذ", en: "In progress" },
  Completed: { ar: "مكتمل", en: "Completed" },
  Cancelled: { ar: "ملغى", en: "Cancelled" },
  Refunded: { ar: "مُسترجَع", en: "Refunded" },
};

const SEND_METHOD_LABEL: Record<string, { ar: string; en: string }> = {
  Broadcast: { ar: "إرسال عام", en: "Broadcast to all" },
  Targeted: { ar: "محامون محددون", en: "Targeted lawyers" },
};

const CONSULTATION_TYPE_LABEL: Record<string, { ar: string; en: string }> = {
  Instant: { ar: "فورية", en: "Instant" },
  Written: { ar: "كتابية", en: "Written" },
  Scheduled: { ar: "مجدولة", en: "Scheduled" },
};

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex items-center justify-between border-b border-border py-3 text-sm last:border-0">
      <span className="text-ink-faint">{label}</span>
      <span className="font-medium text-ink">{children}</span>
    </div>
  );
}

export default function OrderDetail() {
  const { id } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const [searchParams] = useSearchParams();
  // PaymentsReturnController appends ?payment=returned when the gateway hands the payer back, so a
  // fresh mount knows to wait for the webhook instead of offering "Pay by card" all over again.
  const [awaitingGateway, setAwaitingGateway] = useState(searchParams.get("payment") === "returned");
  const [checkoutError, setCheckoutError] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["requestDetail", id],
    queryFn: () => getRequestDetail(id!),
    enabled: !!id,
    // The gateway redirects the payer back the instant they're done, but the webhook that actually
    // flips the status is a separate call landing a beat later. Poll across that gap rather than
    // making them press a button to find out.
    refetchInterval: (q) => (awaitingGateway && q.state.data?.status !== "Paid" ? 2000 : false),
  });

  const invoiceQuery = useQuery({
    queryKey: ["invoice", id],
    queryFn: () => getInvoice(id!),
    enabled: !!id && query.data?.status === "Paid",
  });

  const checkoutMutation = useMutation({
    mutationFn: ({ method, code }: { method: CheckoutMethod; code: string | null }) => checkout(id!, method, code),
    onSuccess: (result) => {
      setCheckoutError(null);
      if (result.paidImmediately) {
        if (isInstant) navigate(`/chat/${id}`);
        else void queryClient.invalidateQueries({ queryKey: ["requestDetail", id] });
      } else if (result.redirectUrl) {
        // Same tab, not a popup: success_url brings them straight back to this page, whereas a
        // popup strands the result in a second tab and trips blockers besides.
        setAwaitingGateway(true);
        window.location.href = result.redirectUrl;
      }
    },
    onError: (error) =>
      setCheckoutError(
        isAxiosError(error) && error.response?.data?.detail === "Insufficient wallet balance."
          ? t("checkout.insufficientWallet")
          : t("checkout.failed"),
      ),
  });

  function refreshAfterGateway() {
    setAwaitingGateway(false);
    void queryClient.invalidateQueries({ queryKey: ["requestDetail", id] });
  }

  const isPaid = query.data?.status === "Paid";
  // An instant consultation is a live call: once paid, the client belongs in the chat, not here.
  const isInstant = query.data?.kind === "Consultation" && query.data.consultationType === "Instant";
  useEffect(() => {
    if (!awaitingGateway) return;
    if (isPaid) {
      setAwaitingGateway(false);
      if (isInstant) navigate(`/chat/${id}`);
      return;
    }
    // A declined payment leaves the request Submitted, so nothing would ever stop the poll on its
    // own — give up after the timeout and fall back to the manual check.
    const timer = setTimeout(() => setAwaitingGateway(false), GATEWAY_POLL_TIMEOUT_MS);
    return () => clearTimeout(timer);
  }, [awaitingGateway, isPaid, isInstant, navigate, id]);

  return (
    <AppShell>
      <Link to="/orders" className="mb-6 inline-flex items-center gap-1.5 text-sm text-ink-faint hover:text-ink">
        {isAr ? <ArrowRight className="h-4 w-4" /> : <ArrowLeft className="h-4 w-4" />}
        {isAr ? "الرجوع إلى طلباتي" : "Back to my orders"}
      </Link>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}
      {query.isError && (
        <p className="text-sm text-rubric">{isAr ? "تعذّر تحميل الطلب." : "Could not load this order."}</p>
      )}

      {query.data && (
        <div className="mx-auto max-w-2xl">
          <div className="mb-4 flex items-start justify-between gap-4">
            <div>
              <h1 className="font-display text-2xl font-bold">
                {isAr ? query.data.serviceNameAr : query.data.serviceNameEn}
              </h1>
              <p className="mt-1 text-sm text-ink-faint">
                <Ltr className="font-mono">{query.data.number}</Ltr>
              </p>
            </div>
            <StatusPill
              status={STATUS_TO_PILL[query.data.status] ?? "draft"}
              label={
                STATUS_LABEL[query.data.status]
                  ? isAr
                    ? STATUS_LABEL[query.data.status].ar
                    : STATUS_LABEL[query.data.status].en
                  : query.data.status
              }
            />
          </div>

          <Card className="mb-4">
            <Row label={isAr ? "العنوان" : "Title"}>{query.data.title ?? "—"}</Row>
            {query.data.specialtyNameAr && (
              <Row label={isAr ? "التخصص" : "Specialty"}>
                {isAr ? query.data.specialtyNameAr : query.data.specialtyNameEn}
              </Row>
            )}
            {query.data.consultationType && (
              <Row label={isAr ? "نوع الاستشارة" : "Consultation type"}>
                {CONSULTATION_TYPE_LABEL[query.data.consultationType]
                  ? isAr
                    ? CONSULTATION_TYPE_LABEL[query.data.consultationType].ar
                    : CONSULTATION_TYPE_LABEL[query.data.consultationType].en
                  : query.data.consultationType}
              </Row>
            )}
            {query.data.selectedDurationMinutes != null && (
              <Row label={isAr ? "المدة" : "Length"}>
                <Ltr className="font-mono">{query.data.selectedDurationMinutes}</Ltr> {isAr ? "دقيقة" : "min"}
              </Row>
            )}
            {query.data.scheduledStartUtc && (
              <Row label={isAr ? "موعد الاستشارة" : "Scheduled for"}>
                <Ltr className="font-mono">{formatDateTime(query.data.scheduledStartUtc)}</Ltr>
              </Row>
            )}
            {query.data.lawyerFullName && (
              <Row label={isAr ? "المحامي" : "Lawyer"}>{query.data.lawyerFullName}</Row>
            )}
            {query.data.variantNameAr && (
              <Row label={isAr ? "الخيار" : "Variant"}>
                {isAr ? query.data.variantNameAr : query.data.variantNameEn}
                {query.data.quantity != null && ` × ${query.data.quantity}`}
              </Row>
            )}
            {query.data.sendMethod && (
              <Row label={isAr ? "طريقة الإرسال" : "Send method"}>
                {SEND_METHOD_LABEL[query.data.sendMethod]
                  ? isAr
                    ? SEND_METHOD_LABEL[query.data.sendMethod].ar
                    : SEND_METHOD_LABEL[query.data.sendMethod].en
                  : query.data.sendMethod}
              </Row>
            )}
            {query.data.awardedLawyerFullName && (
              <Row label={isAr ? "المحامي الفائز" : "Awarded lawyer"}>{query.data.awardedLawyerFullName}</Row>
            )}
            {query.data.subtotal != null && (
              <Row label={isAr ? "المبلغ" : "Amount"}>
                <Ltr className="font-mono">{formatCurrency(query.data.subtotal)}</Ltr>
              </Row>
            )}
            <Row label={isAr ? "تاريخ الإنشاء" : "Created"}>
              <Ltr className="font-mono">{formatDateTime(query.data.createdAtUtc)}</Ltr>
            </Row>
            {query.data.submittedAtUtc && (
              <Row label={isAr ? "تاريخ التقديم" : "Submitted"}>
                <Ltr className="font-mono">{formatDateTime(query.data.submittedAtUtc)}</Ltr>
              </Row>
            )}
            {query.data.cancelledAtUtc && (
              <Row label={isAr ? "تاريخ الإلغاء" : "Cancelled"}>
                <Ltr className="font-mono">{formatDateTime(query.data.cancelledAtUtc)}</Ltr>
              </Row>
            )}
            {query.data.cancelReason && (
              <Row label={isAr ? "سبب الإلغاء" : "Cancel reason"}>{query.data.cancelReason}</Row>
            )}
          </Card>

          {query.data.kind === "Bidding" && query.data.status === "Submitted" && (
            <div className="mb-4">
              <OfferInbox requestId={query.data.id} />
            </div>
          )}

          {(query.data.status === "Submitted" || query.data.status === "Awarded") && query.data.subtotal != null &&
            (awaitingGateway ? (
              <Card className="mb-4 flex flex-col gap-3">
                <p className="text-sm text-ink-soft">{t("checkout.confirming")}</p>
                <Button variant="secondary" onClick={refreshAfterGateway}>
                  {t("checkout.checkStatus")}
                </Button>
              </Card>
            ) : (
              <CheckoutPanel
                requestId={query.data.id}
                isConsultation={query.data.kind === "Consultation"}
                isInstant={isInstant}
                pending={checkoutMutation.isPending}
                error={checkoutError}
                onCheckout={(method, code) => checkoutMutation.mutate({ method, code })}
              />
            ))}

          {query.data.status === "Paid" && invoiceQuery.data && (
            <Card className="mb-4">
              <h2 className="mb-3 text-sm font-semibold text-ink-soft">
                {isAr ? "الفاتورة" : "Invoice"}
              </h2>
              <Row label={isAr ? "رقم الفاتورة" : "Invoice number"}>
                <Ltr className="font-mono">{invoiceQuery.data.number}</Ltr>
              </Row>
              <Row label={isAr ? "المبلغ قبل الضريبة" : "Subtotal (ex. VAT)"}>
                <Ltr className="font-mono">{formatCurrency(invoiceQuery.data.subtotalExVat)}</Ltr>
              </Row>
              {invoiceQuery.data.discountAmount > 0 && (
                <Row label={t("checkout.discount")}>
                  <Ltr className="font-mono text-seal">{formatCurrency(-invoiceQuery.data.discountAmount)}</Ltr>
                </Row>
              )}
              <Row label={isAr ? "ضريبة القيمة المضافة" : "VAT"}>
                <Ltr className="font-mono">{formatCurrency(invoiceQuery.data.vatAmount)}</Ltr>
              </Row>
              <Row label={isAr ? "الإجمالي" : "Total"}>
                <Ltr className="font-mono">{formatCurrency(invoiceQuery.data.total)}</Ltr>
              </Row>
              {invoiceQuery.data.sellerVatNumber && (
                <Row label={isAr ? "الرقم الضريبي" : "VAT number"}>
                  <Ltr className="font-mono">{invoiceQuery.data.sellerVatNumber}</Ltr>
                </Row>
              )}
            </Card>
          )}

          {query.data.status === "Paid" && (query.data.kind === "Consultation" || query.data.kind === "Bidding") && (
            <Link to={`/chat/${query.data.id}`} className="mb-4 block">
              <Card className="flex items-center justify-center gap-2 text-sm font-medium text-seal transition-colors hover:border-seal">
                <MessageCircle className="h-4 w-4" />
                {isAr ? "الدردشة مع المحامي" : "Chat with the lawyer"}
              </Card>
            </Link>
          )}

          {query.data.description && (
            <Card className="mb-4">
              <h2 className="mb-2 text-sm font-semibold text-ink-soft">
                {isAr ? "التفاصيل" : "Details"}
              </h2>
              <p className="whitespace-pre-wrap text-sm text-ink">{query.data.description}</p>
            </Card>
          )}

          {query.data.attachments.length > 0 && (
            <Card>
              <h2 className="mb-3 text-sm font-semibold text-ink-soft">
                {isAr ? "المرفقات" : "Attachments"}
              </h2>
              <div className="flex flex-col gap-2">
                {query.data.attachments.map((a) => (
                  <div key={a.id} className="flex items-center justify-between gap-3 text-sm">
                    <span className="flex items-center gap-2 text-ink">
                      <Paperclip className="h-4 w-4 text-ink-faint" />
                      {a.fileName}
                    </span>
                    <span className="font-mono text-xs text-ink-faint">{a.scanStatus}</span>
                  </div>
                ))}
              </div>
            </Card>
          )}
        </div>
      )}
    </AppShell>
  );
}
