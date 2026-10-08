import { useState, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, ArrowRight, ChevronDown, Info, TrendingDown, TriangleAlert, Wallet as WalletIcon } from "lucide-react";
import { Button, Card, Ltr, StatusTag } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDate, formatDateTime } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { CopyCodeButton } from "../components/CopyCodeButton";
import { REFUND_REASONS, getPaymentDetail, refundPayment, releasePayout, type AdminPaymentDetailDto, type PayoutLineDto, type RefundBearer, type RefundReason } from "../lib/financeApi";

function Row({ label, children }: { label: ReactNode; children: ReactNode }) {
  return (
    <div className="flex items-center justify-between border-b border-border py-3 text-sm last:border-0">
      <span className="text-ink-faint">{label}</span>
      <span className="font-medium text-ink">{children}</span>
    </div>
  );
}

const linkClass = "text-seal-strong underline-offset-4 hover:underline";

function Money({ value, className = "", signed = false }: { value: number; className?: string; signed?: boolean }) {
  return <Ltr className={"font-mono tabular-nums " + className}>{signed && value > 0 ? "+" : ""}{formatCurrency(value)}</Ltr>;
}

function Stat({ label, value, tone = "" }: { label: string; value: number; tone?: string }) {
  return (
    <div className="flex flex-col gap-1 border-border px-5 py-4 sm:border-s sm:first:border-s-0 max-sm:border-t max-sm:first:border-t-0">
      <span className="text-xs text-ink-faint">{label}</span>
      <Money value={value} className={"self-end text-2xl font-semibold " + tone} />
    </div>
  );
}

function SectionTitle({ children, hint }: { children: ReactNode; hint?: string }) {
  return (
    <div className="mb-2">
      <h2 className="font-display text-base font-bold">{children}</h2>
      {hint && <p className="text-xs text-ink-faint">{hint}</p>}
    </div>
  );
}

function Fact({ children }: { children: ReactNode }) {
  return <span className="rounded-md border border-rule bg-paper px-2.5 py-1">{children}</span>;
}

/** Who got what of the money the client paid, before refunds, the refunds' effect, and now.
 * The platform is one net line: its commission minus the discount it funded. */
function SplitCard({ p }: { p: AdminPaymentDetailDto }) {
  const { t } = useTranslation();
  const done = p.refunds.filter((r) => r.status === "Completed");
  const sum = (f: (r: AdminPaymentDetailDto["refunds"][number]) => number) => done.reduce((s, r) => s + f(r), 0);
  const platformAtPayment = p.commissionAmount - p.discountSupport;
  const rows = [
    { key: "vat", label: t("paymentView.vat"), hint: t("paymentView.vatHint"), dot: "bg-warning", before: p.vatAmount, change: -sum((r) => r.vatPortion) },
    { key: "lawyer", label: t("paymentView.lawyerShare"), hint: p.discountSupport > 0 ? t("paymentView.lawyerShareHint") : undefined, dot: "bg-info", before: p.netToLawyerAmount, change: -sum((r) => r.lawyerPortion) },
    {
      key: "platform", label: t("paymentView.platformNet"), dot: "bg-seal-strong", before: platformAtPayment,
      hint: p.discountSupport > 0 ? t("paymentView.platformNetHint", { commission: formatCurrency(p.commissionAmount), support: formatCurrency(p.discountSupport) }) : undefined,
      change: sum((r) => r.discountSupportPortion - r.commissionPortion),
    },
  ];
  const refunded = sum((r) => r.amount);
  const platformNow = platformAtPayment + rows[2].change;

  return (
    <Card className="mb-4">
      <details className="group">
        <summary className="flex cursor-pointer list-none items-center justify-between gap-3 [&::-webkit-details-marker]:hidden">
          <SectionTitle hint={t("paymentView.splitHint")}>{t("paymentView.split")}</SectionTitle>
          <span className="flex shrink-0 items-center gap-2 text-sm text-ink-soft">
            {t("paymentView.clientPaid")} <Money value={p.total - refunded} className="font-semibold" />
            <ChevronDown className="h-5 w-5 text-ink-faint transition-transform group-open:rotate-180" />
          </span>
        </summary>
        <div className="mt-2">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[520px] text-sm">
              <thead>
                <tr className="border-b border-border text-xs text-ink-faint">
                  <th className="py-2 text-start font-medium">{t("paymentView.party")}</th>
                  <th className="py-2 text-center font-medium">{t("paymentView.atPayment")}</th>
                  <th className="py-2 text-center font-medium">{t("paymentView.refundCol")}</th>
                  <th className="py-2 text-center font-medium">{t("paymentView.now")}</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((r) => (
                  <tr key={r.key} className="border-b border-rule">
                    <td className="py-2.5">
                      <span className={"me-2 inline-block h-2.5 w-2.5 rounded-full " + r.dot} />
                      {r.label}
                      {r.hint && <span className="block text-xs text-ink-faint">{r.hint}</span>}
                    </td>
                    <td className="py-2.5 text-center"><Money value={r.before} className={r.before < 0 ? "text-rubric" : ""} /></td>
                    <td className="py-2.5 text-center">
                      {r.change !== 0 ? <Money value={r.change} signed className={r.change < 0 ? "text-rubric" : "text-success"} /> : <span className="text-ink-faint">—</span>}
                    </td>
                    <td className="py-2.5 text-center"><Money value={r.before + r.change} className={r.before + r.change < 0 ? "text-rubric" : ""} /></td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="font-semibold">
                  <td className="pt-2.5">{t("paymentView.clientPaid")}</td>
                  <td className="pt-2.5 text-center"><Money value={p.total} /></td>
                  <td className="pt-2.5 text-center">{refunded > 0 ? <Money value={-refunded} className="text-rubric" /> : <span className="text-ink-faint">—</span>}</td>
                  <td className="pt-2.5 text-center"><Money value={p.total - refunded} /> ✓</td>
                </tr>
              </tfoot>
            </table>
          </div>
          {platformNow < 0 && (
            <p className="mt-3 flex gap-2 rounded-md border border-warning/40 bg-warning-tint px-3 py-2 text-sm text-ink-soft">
              <TrendingDown className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
              {t("paymentView.platformLoss", { amount: formatCurrency(-platformNow) })}
            </p>
          )}
          {refunded > 0 && <p className="mt-2 text-xs text-ink-faint">{t("paymentView.proportional")}</p>}
        </div>
      </details>
    </Card>
  );
}

/** Refunding after the lawyer was paid: who was paid and when, and how much of the refund period is left. */
function RefundWindowNotice({ payout, lawyerName, window }: { payout: PayoutLineDto; lawyerName: string | null; window: NonNullable<AdminPaymentDetailDto["refundWindow"]> }) {
  const { t } = useTranslation();
  const released = new Date(payout.releasedAtUtc ?? payout.createdAtUtc).getTime();
  const elapsed = Math.min(window.windowDays, Math.max(0, Math.floor((Date.now() - released) / 86_400_000)));
  if (window.expired) {
    return (
      <p className="mb-3 flex gap-2 rounded-md border border-rubric/40 bg-rubric-tint px-3 py-2 text-sm text-ink-soft">
        <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0 text-rubric" />
        {t("clawback.expired", { days: window.windowDays, date: formatDate(window.deadlineUtc) })}
      </p>
    );
  }
  return (
    <div className="mb-3 flex flex-col gap-2">
      <p className="flex gap-2 rounded-md border border-warning/40 bg-warning-tint px-3 py-2 text-sm text-ink-soft">
        <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
        {t("clawback.alreadyPaid", { amount: formatCurrency(payout.amount - payout.debtOffset), lawyer: lawyerName ?? "—", date: formatDate(payout.releasedAtUtc ?? payout.createdAtUtc) })}
      </p>
      <div>
        <p className="text-xs text-ink-soft">{t("clawback.windowUsed", { elapsed, days: window.windowDays })}</p>
        <div className="my-1 h-2 overflow-hidden rounded-full bg-surface-raised" aria-hidden>
          <span className="block h-full bg-warning" style={{ width: `${(elapsed / window.windowDays) * 100}%` }} />
        </div>
        <p className="text-xs text-ink-faint">{t("clawback.deadline", { date: formatDate(window.deadlineUtc) })}</p>
      </div>
    </div>
  );
}

/** A reference number: monospace, LTR, with a copy button. */
function Ref({ value }: { value: string }) {
  return (
    <span className="flex items-center gap-1">
      <Ltr className="font-mono text-xs">{value}</Ltr>
      <CopyCodeButton code={value} />
    </span>
  );
}

export default function PaymentDetail() {
  const { id } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const [refundAmount, setRefundAmount] = useState("");
  const [refundReason, setRefundReason] = useState<RefundReason | "">("");
  const [refundDetails, setRefundDetails] = useState("");
  const [bearer, setBearer] = useState<RefundBearer>("Lawyer");
  const [showRefundForm, setShowRefundForm] = useState(false);
  const [refundTried, setRefundTried] = useState(false);
  const [releaseTried, setReleaseTried] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showRelease, setShowRelease] = useState(false);
  const [releaseReason, setReleaseReason] = useState("");

  const query = useQuery({ queryKey: ["adminPaymentDetail", id], queryFn: () => getPaymentDetail(id!), enabled: !!id });

  function invalidate() {
    void queryClient.invalidateQueries({ queryKey: ["adminPaymentDetail", id] });
  }

  const refundMutation = useMutation({
    mutationFn: () => refundPayment(id!, Number(refundAmount), refundReason as RefundReason, refundDetails, afterPayout ? bearer : undefined),
    onSuccess: () => {
      setError(null);
      setShowRefundForm(false);
      setRefundAmount("");
      setRefundReason("");
      setRefundDetails("");
      setRefundTried(false);
      invalidate();
    },
    onError: () => setError(t("refund.failed")),
  });

  const releasePayoutMutation = useMutation({
    mutationFn: (payoutId: string) => releasePayout(payoutId, releaseReason.trim()),
    onSuccess: () => {
      setError(null);
      setShowRelease(false);
      setReleaseReason("");
      setReleaseTried(false);
      invalidate();
    },
    onError: () => setError(t("payout.failed")),
  });

  const p = query.data;
  const refundedSoFar = p?.refunds.filter((r) => r.status === "Completed").reduce((sum, r) => sum + r.amount, 0) ?? 0;
  const remaining = p ? Math.round((p.total - refundedSoFar) * 100) / 100 : 0;
  const amountNum = Number(refundAmount);
  const refundAmountError = !refundAmount.trim()
    ? t("form.required")
    : !(amountNum > 0 && amountNum <= remaining) ? t("refund.amountInvalid", { amount: formatCurrency(remaining) }) : null;
  const refundReasonError = refundReason ? null : t("form.required");
  const refundDetailsError = refundReason === "Other" && !refundDetails.trim() ? t("refund.detailsRequired") : null;
  const releaseReasonError = !releaseReason.trim() ? t("form.required") : releaseReason.trim().length < 10 ? t("payout.reasonTooShort") : null;
  const doneRefunds = p?.refunds.filter((r) => r.status === "Completed") ?? [];
  // Before release the share came out of the held amount; after it, it became a debt or a platform loss.
  const lawyerRefunded = doneRefunds.filter((r) => !r.lawyerShareBearer).reduce((sum, r) => sum + r.lawyerPortion, 0);
  const clawedBack = doneRefunds.filter((r) => r.lawyerShareBearer === "Lawyer").reduce((sum, r) => sum + r.lawyerPortion, 0);
  const platformAbsorbed = doneRefunds.filter((r) => r.lawyerShareBearer === "Platform").reduce((sum, r) => sum + r.lawyerPortion, 0);
  const afterPayout = p?.payout?.status === "Released";
  const windowExpired = !!p?.refundWindow?.expired;
  const canRefund = p && (p.status === "Paid" || p.status === "PartiallyRefunded");
  const estimatedLawyerShare = p && p.total > 0 ? (p.netToLawyerAmount * (Number(refundAmount) || 0)) / p.total : 0;

  return (
    <AppShell>
      <Link to="/payments" className="mb-6 inline-flex items-center gap-1.5 text-sm text-ink-faint hover:text-ink">
        {isAr ? <ArrowRight className="h-4 w-4" /> : <ArrowLeft className="h-4 w-4" />}
        {isAr ? "الرجوع إلى المدفوعات" : "Back to payments"}
      </Link>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}

      {p && (
        <div className="mx-auto max-w-2xl">
          <div className="mb-4 flex items-start justify-between gap-4">
            <div>
              <h1 className="font-display text-2xl font-bold">{p.number}</h1>
              <p className="mt-1 text-sm text-ink-faint">
                {t(`paymentsAdmin.purposes.${p.purpose}`, p.purpose)}
                {p.requestType && ` · ${t(`paymentView.requestTypes.${p.requestType}`, p.requestType)}`}
              </p>
            </div>
            <StatusTag status={p.status} label={t(`paymentStatus.${p.status}`, p.status)} />
          </div>

          {/* The three numbers that matter. */}
          <div className="mb-4 grid overflow-hidden rounded-xl border border-border bg-surface sm:grid-cols-3">
            <Stat label={t("paymentView.clientPaid")} value={p.total} />
            <Stat label={t("paymentView.refundedToClient")} value={-refundedSoFar} tone="text-rubric" />
            <Stat label={t("paymentView.netFromClient")} value={p.total - refundedSoFar} tone="text-seal-strong" />
          </div>

          <Card className="mb-4">
            <SectionTitle>{t("paymentView.parties")}</SectionTitle>
            <div className="grid gap-x-8 sm:grid-cols-2">
              <div>
                {/* clientName falls back to a raw E.164 phone number when the client never set a
                    display name (phone-first accounts) — that "+" needs <Ltr> inside RTL text. */}
                <Row label={isAr ? "العميل" : "Client"}>
                  {p.clientName || p.clientPhoneE164 ? (
                    <Link to={`/clients/${p.clientProfileId}`} className={linkClass}>
                      {p.clientName?.startsWith("+") ? <Ltr className="font-mono">{p.clientName}</Ltr> : p.clientName}
                    </Link>
                  ) : "—"}
                </Row>
                <Row label={isAr ? "المحامي" : "Lawyer"}>
                  {p.lawyerName && p.lawyerProfileId ? <Link to={`/lawyers/${p.lawyerProfileId}`} className={linkClass}>{p.lawyerName}</Link> : p.lawyerName ?? "—"}
                </Row>
                <Row label={isAr ? "رقم الطلب" : "Request"}>
                  {p.requestNumber && p.serviceRequestId ? (
                    <Link to={`/requests/${p.serviceRequestId}`} className={linkClass}>
                      <Ltr className="font-mono">{p.requestNumber}</Ltr>
                    </Link>
                  ) : "—"}
                </Row>
              </div>
              <div>
                <Row label={isAr ? "طريقة الدفع" : "Method"}>
                  {t(`paymentsAdmin.methods.${p.methodDescription}`, p.methodDescription)}
                  {p.transaction?.cardMasked && <Ltr className="ms-2 font-mono text-xs text-ink-faint">•••• {p.transaction.cardMasked.slice(-4)}</Ltr>}
                </Row>
                <Row label={isAr ? "تاريخ الإنشاء" : "Created"}><Ltr className="font-mono">{formatDateTime(p.createdAtUtc)}</Ltr></Row>
                {p.paidAtUtc && <Row label={isAr ? "تاريخ الدفع" : "Paid"}><Ltr className="font-mono">{formatDateTime(p.paidAtUtc)}</Ltr></Row>}
              </div>
            </div>
            {p.failureReason && <Row label={isAr ? "سبب الفشل" : "Failure reason"}>{p.failureReason}</Row>}
          </Card>

          {p.discount && (
            <Card className="mb-4">
              <SectionTitle hint={t("paymentView.discountHint", { paid: formatCurrency(p.total), price: formatCurrency(p.grossAmount) })}>
                {t("paymentView.priceAndDiscount")}
              </SectionTitle>
              <Row label={t("paymentView.price")}><Money value={p.grossAmount} /></Row>
              <Row label={<>{t("paymentView.code")} <Ltr className="ms-1 rounded border border-dashed border-seal bg-seal-tint px-1.5 font-mono text-xs text-seal-strong">{p.discount.code}</Ltr></>}>
                <Money value={-p.discountAmount} className="text-rubric" />
              </Row>
              <Row label={<b className="text-ink">{t("paymentView.clientPaid")}</b>}><Money value={p.total} className="font-semibold" /></Row>
              <div className="mt-3 flex flex-wrap gap-2 text-xs text-ink-soft">
                <Fact>
                  {p.discount.kind === "Percentage"
                    ? t("paymentView.percentOff", { value: p.discount.value })
                    : t("paymentView.fixedOff", { value: formatCurrency(p.discount.value) })}
                </Fact>
                {p.discount.maxDiscountAmount != null && <Fact>{t("paymentView.cap")} <Money value={p.discount.maxDiscountAmount} /></Fact>}
                <Fact>{t("paymentView.appliesTo")} {p.discount.scopes.map((s) => t(`discount.scopes.${s}`, s)).join("، ")}</Fact>
                <Link to={`/discount-codes/${p.discount.id}`} className="rounded-md border border-rule bg-paper px-2.5 py-1 text-seal-strong hover:underline">
                  {t("paymentView.openCode")}
                </Link>
              </div>
              <p className="mt-3 flex gap-2 rounded-md border border-info/30 bg-info-tint px-3 py-2 text-sm text-ink-soft">
                <Info className="mt-0.5 h-4 w-4 shrink-0 text-info" />
                {t("paymentView.platformFunded", { support: formatCurrency(p.discountSupport) })}
              </p>
            </Card>
          )}

          <Card className="mb-4">
            <details className="group">
              {/* Collapsed by default; the summary line still says what kind of payment it was. */}
              <summary className="flex cursor-pointer list-none items-center justify-between gap-3 [&::-webkit-details-marker]:hidden">
                <h2 className="text-sm font-semibold text-ink-soft">{t("bankTx.title")}</h2>
                <span className="flex items-center gap-2 text-xs text-ink-faint">
                  {p.methodDescription === "Wallet" ? (
                    t("paymentsAdmin.methods.Wallet")
                  ) : p.transaction ? (
                    <>
                      {p.transaction.cardBrand && t(`bankTx.brands.${p.transaction.cardBrand}`, p.transaction.cardBrand)}
                      {p.transaction.cardMasked && <Ltr className="font-mono">•••• {p.transaction.cardMasked.slice(-4)}</Ltr>}
                    </>
                  ) : null}
                  <ChevronDown className="h-4 w-4 transition-transform group-open:rotate-180" />
                </span>
              </summary>
              <div className="mt-3">
            {p.methodDescription === "Wallet" ? (
              <>
                <p className="py-2 text-sm text-ink-soft">{t("bankTx.wallet")}</p>
                {p.walletTransactionId && (
                  <Row label={t("bankTx.walletTx")}><Ref value={p.walletTransactionId} /></Row>
                )}
              </>
            ) : (
              <>
                <Row label={t("bankTx.gateway")}>{t(`bankTx.gateways.${p.gatewayProvider}`, p.gatewayProvider || "—")}</Row>
                {p.transaction ? (
                  <>
                    <Row label={t("bankTx.source")}>
                      <span className="flex items-center gap-2">
                        {[p.transaction.cardBrand && t(`bankTx.brands.${p.transaction.cardBrand}`, p.transaction.cardBrand),
                          p.transaction.sourceType && p.transaction.sourceType !== "creditcard" && t(`bankTx.sources.${p.transaction.sourceType}`, p.transaction.sourceType)]
                          .filter(Boolean).join(" · ") || "—"}
                        {p.transaction.cardMasked && <Ltr className="font-mono text-ink-soft">{p.transaction.cardMasked}</Ltr>}
                      </span>
                    </Row>
                    {p.transaction.referenceNumber && <Row label={t("bankTx.rrn")}><Ref value={p.transaction.referenceNumber} /></Row>}
                    {p.transaction.authorizationCode && <Row label={t("bankTx.authCode")}><Ref value={p.transaction.authorizationCode} /></Row>}
                    {(p.transaction.message || p.transaction.responseCode) && (
                      <Row label={t("bankTx.bankResponse")}>
                        <Ltr className="font-mono">{[p.transaction.message, p.transaction.responseCode && `(${p.transaction.responseCode})`].filter(Boolean).join(" ")}</Ltr>
                      </Row>
                    )}
                    {p.transaction.fee != null && (
                      <Row label={t("bankTx.fee")}><Ltr className="font-mono">{formatCurrency(p.transaction.fee)}</Ltr></Row>
                    )}
                  </>
                ) : (
                  <p className="py-2 text-sm text-ink-faint">{p.gatewayPaymentId ? t("bankTx.notAvailable") : t("bankTx.noGatewayRef")}</p>
                )}
                {p.gatewayPaymentId && <Row label={t("bankTx.invoiceId")}><Ref value={p.gatewayPaymentId} /></Row>}
                {p.transaction?.transactionId && <Row label={t("bankTx.transactionId")}><Ref value={p.transaction.transactionId} /></Row>}
                {p.refunds.filter((r) => r.gatewayRefundId).map((r) => (
                  <Row key={r.id} label={`${t("bankTx.refundRef")} · ${formatCurrency(r.amount)}`}><Ref value={r.gatewayRefundId!} /></Row>
                ))}
                {p.gatewayProvider === "Fake" && <p className="mt-2 text-xs text-warning">{t("bankTx.testNote")}</p>}
              </>
            )}
              </div>
            </details>
          </Card>

          {p.purpose === "RequestCheckout" && p.status !== "Failed" && p.status !== "Initiated" && <SplitCard p={p} />}



          {p.payout && (
            <Card className="mb-4">
              <details className="group">
                <summary className="flex cursor-pointer list-none items-center justify-between gap-3 [&::-webkit-details-marker]:hidden">
                  <h2 className="text-sm font-semibold text-ink-soft">{t("payout.title")}</h2>
                  <span className="flex shrink-0 items-center gap-2 text-sm">
                    <StatusTag status={p.payout.status} label={t(`payout.statuses.${p.payout.status}`, p.payout.status)} />
                    <Money value={p.payout.amount - p.payout.debtOffset} className="font-semibold text-seal-strong" />
                    <ChevronDown className="h-5 w-5 text-ink-faint transition-transform group-open:rotate-180" />
                  </span>
                </summary>
                <div className="mt-3">
                  <Row label={t("paymentView.payoutOriginal")}><Money value={p.netToLawyerAmount} /></Row>
                  {lawyerRefunded > 0 && (
                    <Row label={t("paymentView.payoutDeducted")}><Money value={-lawyerRefunded} className="text-rubric" /></Row>
                  )}
                  {p.payout.debtOffset > 0 && (
                    <Row label={t("clawback.offsetAtRelease")}><Money value={-p.payout.debtOffset} className="text-rubric" /></Row>
                  )}
                  <Row label={<b className="text-ink">{p.payout.status === "Released" ? t("paymentView.payoutPaid") : t("paymentView.payoutNow")}</b>}>
                    <Money value={p.payout.amount - p.payout.debtOffset} className="text-lg font-semibold text-seal-strong" />
                  </Row>
                  {clawedBack > 0 && (
                    <Row label={t("clawback.toRecover")}><Money value={-clawedBack} className="text-rubric" /></Row>
                  )}
                  {platformAbsorbed > 0 && (
                    <Row label={t("clawback.platformAbsorbed")}><Money value={-platformAbsorbed} className="text-rubric" /></Row>
                  )}
                  {(clawedBack > 0 || p.payout.lawyerDebtBalance > 0) && (
                    <p className="mt-2 flex flex-wrap items-center justify-between gap-2 rounded-md bg-warning-tint px-3 py-2 text-sm text-ink-soft">
                      {t("clawback.lawyerOwesNow")} <Money value={p.payout.lawyerDebtBalance} className="font-semibold text-warning" />
                      <Link to={`/lawyer-debts/${p.payout.lawyerProfileId}`} className="text-seal-strong hover:underline">{t("clawback.openDebt")}</Link>
                    </p>
                  )}
                  {p.payout.releasedAtUtc ? (
                    <Row label={t("payout.releasedAt")}>
                      <Ltr className="font-mono">{formatDateTime(p.payout.releasedAtUtc)}</Ltr>
                    </Row>
                  ) : p.payout.status === "Cancelled" ? (
                    <p className="py-2 text-sm text-ink-faint">{t("paymentView.payoutCancelled")}</p>
                  ) : p.payout.status === "Suspended" ? (
                    <p className="py-2 text-sm text-warning">{t("clawback.suspended")}</p>
                  ) : (
                    <Row label={t("payout.status")}>
                      {t("payout.heldFor", { count: Math.floor((Date.now() - new Date(p.payout.createdAtUtc).getTime()) / 86_400_000) })}
                    </Row>
                  )}

                  {(p.payout.status === "Held" || p.payout.status === "Suspended") && !showRelease && (
                    <div className="mt-3 flex flex-wrap items-center justify-between gap-2">
                      <p className="text-xs text-ink-faint">{t("payout.normal")}</p>
                      <button type="button" onClick={() => setShowRelease(true)} className="text-sm text-ink-soft underline-offset-4 hover:text-seal hover:underline">
                        {t("payout.manualToggle")}
                      </button>
                    </div>
                  )}

                  {(p.payout.status === "Held" || p.payout.status === "Suspended") && showRelease && (
                    <form
                      className="mt-4 flex flex-col gap-3 rounded-md border border-warning bg-warning-tint p-4"
                      onSubmit={(e) => {
                        e.preventDefault();
                        setReleaseTried(true);
                        if (!releaseReasonError) releasePayoutMutation.mutate(p.payout!.id);
                      }}
                    >
                      <p className="flex items-center gap-2 text-sm font-semibold text-warning">
                        <TriangleAlert className="h-4 w-4" />
                        {t("payout.manualTitle")}
                      </p>
                      <ul className="list-disc ps-5 text-sm text-ink-soft">
                        <li>{t("payout.warnUndo")}</li>
                        <li>{t("payout.warnRefund")}</li>
                      </ul>
                      <label className="flex flex-col gap-1.5">
                        <span className="text-xs font-medium text-ink-soft">{t("payout.reason")} *</span>
                        <textarea
                          id="release-reason"
                          rows={3}
                          maxLength={500}
                          aria-invalid={releaseTried && !!releaseReasonError}
                          value={releaseReason}
                          onChange={(e) => setReleaseReason(e.target.value)}
                          className={(releaseTried && releaseReasonError ? "border-rubric " : "border-border ") + "rounded-md border bg-surface-raised px-3 py-2 text-sm text-ink focus:border-seal focus:outline-none focus:ring-2 focus:ring-seal/30"}
                        />
                        {releaseTried && releaseReasonError && <span className="text-xs text-rubric">{releaseReasonError}</span>}
                        <span className="text-xs text-ink-faint">{t("payout.reasonHint")}</span>
                      </label>
                      <div className="flex items-center gap-2">
                        <Button type="submit" disabled={releasePayoutMutation.isPending}>
                          <WalletIcon className="h-4 w-4" />
                          {t("payout.confirm")}
                        </Button>
                        <Button type="button" variant="ghost" onClick={() => { setShowRelease(false); setReleaseReason(""); setReleaseTried(false); }}>
                          {t("payout.cancel")}
                        </Button>
                      </div>
                      {error && <p className="text-sm text-rubric">{error}</p>}
                    </form>
                  )}
                </div>
              </details>
            </Card>
          )}

          {p.refunds.length > 0 && (
            <Card className="mb-4">
              <details className="group">
                <summary className="flex cursor-pointer list-none items-center justify-between gap-3 [&::-webkit-details-marker]:hidden">
                  <SectionTitle hint={t("paymentView.refundsHint")}>{isAr ? "الاسترجاعات" : "Refunds"}</SectionTitle>
                  <span className="flex items-center gap-2 text-xs text-ink-faint">
                    {t("paymentView.refundsSummary", { count: p.refunds.length })} · <Money value={refundedSoFar} />
                    <ChevronDown className="h-4 w-4 transition-transform group-open:rotate-180" />
                  </span>
                </summary>
              <div className="mt-3 flex flex-col gap-3">
                {p.refunds.map((r) => {
                  const platformNet = r.discountSupportPortion - r.commissionPortion;
                  return (
                    <div key={r.id} className="overflow-hidden rounded-lg border border-rule bg-paper">
                      <div className="flex flex-wrap items-center gap-x-3 gap-y-1 bg-surface-raised px-4 py-2.5 text-sm">
                        <b className="font-display">{t(`refund.reasons.${r.reason}`, r.reason)}</b>
                        <StatusTag status={r.status} label={t(`refund.statuses.${r.status}`, r.status)} />
                        {r.details && <span className="text-xs text-ink-faint">{r.details}</span>}
                        {r.lawyerShareBearer && (
                          <span className="inline-block rounded-full bg-warning-tint px-2 py-0.5 text-[11px] text-warning">{t(`clawback.refundTag.${r.lawyerShareBearer}`)}</span>
                        )}
                        <Ltr className="ms-auto font-mono text-xs text-ink-faint">{formatDateTime(r.createdAtUtc)}</Ltr>
                      </div>
                      <div className="px-4 pb-2">
                        <div className="flex items-baseline justify-between border-b border-dashed border-rule py-2.5">
                          <span className="text-sm text-ink-soft">{t("paymentView.refundedToClient")}</span>
                          <Money value={r.amount} className="text-xl font-semibold" />
                        </div>
                        <Row label={r.lawyerShareBearer ? t(`clawback.shareLine.${r.lawyerShareBearer}`) : t("paymentView.fromLawyer")}>
                          <Money value={-r.lawyerPortion} className="text-rubric" />
                        </Row>
                        {r.vatPortion > 0 && <Row label={t("paymentView.fromVat")}><Money value={-r.vatPortion} className="text-rubric" /></Row>}
                        <Row label={<>{t("paymentView.platformNet")}<span className="block text-xs text-ink-faint">{t("paymentView.platformNetRefundHint", { commission: formatCurrency(r.commissionPortion), support: formatCurrency(r.discountSupportPortion) })}</span></>}>
                          <Money value={platformNet} className={platformNet >= 0 ? "text-success" : "text-rubric"} signed />
                        </Row>
                        {r.gatewayRefundId && (
                          <Row label={t("bankTx.refundRef")}><Ref value={r.gatewayRefundId} /></Row>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
              </details>
            </Card>
          )}

          {canRefund && (
            <Card>
              <h2 className="mb-3 text-sm font-semibold text-ink-soft">{t("refund.title")}</h2>
              {afterPayout && p.payout && p.refundWindow && (
                <RefundWindowNotice payout={p.payout} lawyerName={p.lawyerName} window={p.refundWindow} />
              )}
              {showRefundForm && !windowExpired ? (
                <form
                  className="flex flex-col gap-3"
                  onSubmit={(e) => {
                    e.preventDefault();
                    setRefundTried(true);
                    if (!refundAmountError && !refundReasonError && !refundDetailsError) refundMutation.mutate();
                  }}
                >
                  <label className="flex flex-col gap-1.5">
                    <span className="text-xs font-medium text-ink-soft">{t("refund.amount")} *</span>
                    <input
                      id="refund-amount"
                      type="number"
                      dir="ltr"
                      min={0}
                      step="0.01"
                      inputMode="decimal"
                      value={refundAmount}
                      aria-invalid={refundTried && !!refundAmountError}
                      onChange={(e) => setRefundAmount(e.target.value)}
                      className={(refundTried && refundAmountError ? "border-rubric " : "border-border ") + "rounded-md border bg-surface-raised px-3 py-2 text-sm"}
                    />
                    {refundTried && refundAmountError ? (
                      <span className="text-xs text-rubric">{refundAmountError}</span>
                    ) : (
                      <span className="text-xs text-ink-faint">{t("refund.remaining", { amount: formatCurrency(remaining) })}</span>
                    )}
                  </label>
                  <label className="flex flex-col gap-1.5">
                    <span className="text-xs font-medium text-ink-soft">{t("refund.reason")} *</span>
                    <select
                      id="refund-reason"
                      value={refundReason}
                      aria-invalid={refundTried && !!refundReasonError}
                      onChange={(e) => setRefundReason(e.target.value as RefundReason | "")}
                      className={(refundTried && refundReasonError ? "border-rubric " : "border-border ") + "rounded-md border bg-surface-raised px-3 py-2 text-sm"}
                    >
                      <option value="">{t("refund.choose")}</option>
                      {REFUND_REASONS.map((r) => (
                        <option key={r} value={r}>{t(`refund.reasons.${r}`)}</option>
                      ))}
                    </select>
                    {refundTried && refundReasonError && <span className="text-xs text-rubric">{refundReasonError}</span>}
                  </label>
                  <label className="flex flex-col gap-1.5">
                    <span className="text-xs font-medium text-ink-soft">
                      {refundReason === "Other" ? `${t("refund.details")} *` : t("refund.detailsOptional")}
                    </span>
                    <textarea
                      id="refund-details"
                      rows={2}
                      maxLength={500}
                      value={refundDetails}
                      aria-invalid={refundTried && !!refundDetailsError}
                      onChange={(e) => setRefundDetails(e.target.value)}
                      className={(refundTried && refundDetailsError ? "border-rubric " : "border-border ") + "rounded-md border bg-surface-raised px-3 py-2 text-sm"}
                    />
                    {refundTried && refundDetailsError && <span className="text-xs text-rubric">{refundDetailsError}</span>}
                  </label>
                  {afterPayout && (
                    <fieldset className="flex flex-col gap-2">
                      <legend className="mb-1.5 text-xs font-medium text-ink-soft">{t("clawback.whoBears")} *</legend>
                      <div className="grid gap-2 sm:grid-cols-2" role="radiogroup">
                        {(["Lawyer", "Platform"] as const).map((b) => (
                          <button
                            key={b}
                            type="button"
                            role="radio"
                            aria-checked={bearer === b}
                            onClick={() => setBearer(b)}
                            className={
                              "flex flex-col gap-1 rounded-md border p-3 text-start " +
                              (bearer === b ? "border-seal bg-seal-tint" : "border-border bg-paper hover:border-seal")
                            }
                          >
                            <b className="font-display text-sm">{t(`clawback.bearer.${b}`)}</b>
                            <span className="text-xs text-ink-faint">{t(`clawback.bearerHint.${b}`)}</span>
                          </button>
                        ))}
                      </div>
                      {Number(refundAmount) > 0 && (
                        <p className="rounded-md border border-dashed border-border bg-paper px-3 py-2 text-sm text-ink-soft">
                          {t(bearer === "Lawyer" ? "clawback.previewLawyer" : "clawback.previewPlatform", { amount: formatCurrency(estimatedLawyerShare) })}
                        </p>
                      )}
                    </fieldset>
                  )}
                  <div className="flex gap-2">
                    <Button type="submit" variant="danger" disabled={refundMutation.isPending}>
                      {t("refund.confirm")}
                    </Button>
                    <Button type="button" variant="ghost" onClick={() => { setShowRefundForm(false); setRefundTried(false); }}>
                      {t("refund.cancel")}
                    </Button>
                  </div>
                </form>
              ) : (
                <div className="flex flex-wrap items-center gap-3">
                  <Button variant="danger" disabled={windowExpired} onClick={() => setShowRefundForm(true)}>
                    {t("refund.issue")}
                  </Button>
                  <span className="text-xs text-ink-faint">{t("refund.remaining", { amount: formatCurrency(remaining) })}</span>
                </div>
              )}
              {error && <p className="mt-2 text-sm text-rubric">{error}</p>}
            </Card>
          )}

        </div>
      )}
    </AppShell>
  );
}
