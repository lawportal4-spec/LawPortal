import { useState, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, ArrowRight, Wallet as WalletIcon } from "lucide-react";
import { Button, Card, Ltr } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDateTime } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getPaymentDetail, refundPayment, releasePayout } from "../lib/financeApi";

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex items-center justify-between border-b border-border py-3 text-sm last:border-0">
      <span className="text-ink-faint">{label}</span>
      <span className="font-medium text-ink">{children}</span>
    </div>
  );
}

export default function PaymentDetail() {
  const { id } = useParams<{ id: string }>();
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const [refundAmount, setRefundAmount] = useState("");
  const [refundReason, setRefundReason] = useState("");
  const [showRefundForm, setShowRefundForm] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const query = useQuery({ queryKey: ["adminPaymentDetail", id], queryFn: () => getPaymentDetail(id!), enabled: !!id });

  function invalidate() {
    void queryClient.invalidateQueries({ queryKey: ["adminPaymentDetail", id] });
  }

  const refundMutation = useMutation({
    mutationFn: () => refundPayment(id!, Number(refundAmount), refundReason),
    onSuccess: () => {
      setError(null);
      setShowRefundForm(false);
      setRefundAmount("");
      setRefundReason("");
      invalidate();
    },
    onError: () => setError(isAr ? "تعذّر تنفيذ الاسترجاع." : "Could not process the refund."),
  });

  const releasePayoutMutation = useMutation({
    mutationFn: (payoutId: string) => releasePayout(payoutId),
    onSuccess: invalidate,
    onError: () => setError(isAr ? "تعذّر تحرير المستحقات." : "Could not release the payout."),
  });

  const p = query.data;
  const refundedSoFar = p?.refunds.filter((r) => r.status === "Completed").reduce((sum, r) => sum + r.amount, 0) ?? 0;
  const canRefund = p && (p.status === "Paid" || p.status === "PartiallyRefunded") && p.payout?.status !== "Released";

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
              <p className="mt-1 text-sm text-ink-faint">{p.purpose}</p>
            </div>
            <span className="rounded-full bg-paper px-3 py-1 text-xs font-medium text-ink-soft">{p.status}</span>
          </div>

          <Card className="mb-4">
            {/* clientName falls back to a raw E.164 phone number when the client never set a
                display name (phone-first, passwordless accounts) — that "+" needs <Ltr> or it
                visually migrates to the wrong end inside an RTL paragraph. */}
            <Row label={isAr ? "العميل" : "Client"}>
              {p.clientName?.startsWith("+") ? <Ltr className="font-mono">{p.clientName}</Ltr> : p.clientName ?? "—"}
            </Row>
            <Row label={isAr ? "المحامي" : "Lawyer"}>{p.lawyerName ?? "—"}</Row>
            <Row label={isAr ? "رقم الطلب" : "Request"}>{p.requestNumber ?? "—"}</Row>
            <Row label={isAr ? "طريقة الدفع" : "Method"}>{p.methodDescription}</Row>
            <Row label={isAr ? "الإجمالي" : "Total"}>
              <Ltr className="font-mono">{formatCurrency(p.total)}</Ltr>
            </Row>
            {p.isVatApplicable && (
              <Row label={isAr ? "ضريبة القيمة المضافة" : "VAT"}>
                <Ltr className="font-mono">{formatCurrency(p.vatAmount)}</Ltr>
              </Row>
            )}
            <Row label={isAr ? "العمولة" : "Commission"}>
              <Ltr className="font-mono">{formatCurrency(p.commissionAmount)}</Ltr>
            </Row>
            <Row label={isAr ? "صافي المحامي" : "Net to lawyer"}>
              <Ltr className="font-mono">{formatCurrency(p.netToLawyerAmount)}</Ltr>
            </Row>
            <Row label={isAr ? "تاريخ الإنشاء" : "Created"}>
              <Ltr className="font-mono">{formatDateTime(p.createdAtUtc)}</Ltr>
            </Row>
            {p.paidAtUtc && (
              <Row label={isAr ? "تاريخ الدفع" : "Paid"}>
                <Ltr className="font-mono">{formatDateTime(p.paidAtUtc)}</Ltr>
              </Row>
            )}
            {p.failureReason && <Row label={isAr ? "سبب الفشل" : "Failure reason"}>{p.failureReason}</Row>}
          </Card>

          {p.payout && (
            <Card className="mb-4">
              <h2 className="mb-3 text-sm font-semibold text-ink-soft">{isAr ? "المستحقات" : "Payout"}</h2>
              <Row label={isAr ? "المبلغ" : "Amount"}>
                <Ltr className="font-mono">{formatCurrency(p.payout.amount)}</Ltr>
              </Row>
              <Row label={isAr ? "الحالة" : "Status"}>{p.payout.status}</Row>
              {p.payout.status === "Held" && (
                <Button className="mt-3" onClick={() => releasePayoutMutation.mutate(p.payout!.id)} disabled={releasePayoutMutation.isPending}>
                  <WalletIcon className="h-4 w-4" />
                  {isAr ? "تحرير المستحقات" : "Release payout"}
                </Button>
              )}
            </Card>
          )}

          {p.refunds.length > 0 && (
            <Card className="mb-4">
              <h2 className="mb-3 text-sm font-semibold text-ink-soft">{isAr ? "الاسترجاعات" : "Refunds"}</h2>
              <div className="flex flex-col gap-2">
                {p.refunds.map((r) => (
                  <div key={r.id} className="flex items-center justify-between text-sm">
                    <span className="text-ink-soft">{r.reason}</span>
                    <span className="font-mono">
                      <Ltr>{formatCurrency(r.amount)}</Ltr> · {r.status}
                    </span>
                  </div>
                ))}
              </div>
            </Card>
          )}

          {canRefund && (
            <Card>
              <h2 className="mb-3 text-sm font-semibold text-ink-soft">{isAr ? "استرجاع" : "Refund"}</h2>
              {showRefundForm ? (
                <div className="flex flex-col gap-3">
                  <input
                    type="number"
                    value={refundAmount}
                    onChange={(e) => setRefundAmount(e.target.value)}
                    placeholder={isAr ? `المبلغ (المتبقي: ${formatCurrency(p.total - refundedSoFar)})` : `Amount (remaining: ${formatCurrency(p.total - refundedSoFar)})`}
                    className="rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
                  />
                  <input
                    value={refundReason}
                    onChange={(e) => setRefundReason(e.target.value)}
                    placeholder={isAr ? "السبب" : "Reason"}
                    className="rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
                  />
                  <div className="flex gap-2">
                    <Button variant="danger" disabled={!refundAmount || !refundReason.trim() || refundMutation.isPending} onClick={() => refundMutation.mutate()}>
                      {isAr ? "تأكيد الاسترجاع" : "Confirm refund"}
                    </Button>
                    <Button variant="ghost" onClick={() => setShowRefundForm(false)}>
                      {isAr ? "إلغاء" : "Cancel"}
                    </Button>
                  </div>
                </div>
              ) : (
                <Button variant="danger" onClick={() => setShowRefundForm(true)}>
                  {isAr ? "إصدار استرجاع" : "Issue a refund"}
                </Button>
              )}
              {error && <p className="mt-2 text-sm text-rubric">{error}</p>}
            </Card>
          )}
        </div>
      )}
    </AppShell>
  );
}
