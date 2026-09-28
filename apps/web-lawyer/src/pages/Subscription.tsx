import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { Check, CreditCard, Megaphone, Percent, TriangleAlert } from "lucide-react";
import { Button, Card, Ltr, StatusTag } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDate } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import {
  cancelSubscription,
  getMySubscription,
  getMySubscriptionInvoices,
  getSubscriptionPlans,
  payInvoice,
  subscribeToPlan,
} from "../lib/subscriptionApi";

/** Same bound as web-client's OrderDetail: a declined payment leaves the invoice Pending, so
 * nothing would stop the poll on its own. */
const GATEWAY_POLL_TIMEOUT_MS = 90_000;

const STATUS_LABEL: Record<string, { ar: string; en: string }> = {
  Active: { ar: "نشطة", en: "Active" },
  PastDue: { ar: "بانتظار الدفع", en: "Payment due" },
  Canceled: { ar: "مُلغاة", en: "Canceled" },
  Expired: { ar: "منتهية", en: "Expired" },
};

const INVOICE_STATUS_LABEL: Record<string, { ar: string; en: string }> = {
  Pending: { ar: "بانتظار الدفع", en: "Pending" },
  Paid: { ar: "مدفوعة", en: "Paid" },
  Failed: { ar: "فشلت", en: "Failed" },
};

export default function Subscription() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [searchParams] = useSearchParams();
  // PaymentsReturnController sends a subscription payer back here with ?payment=returned.
  const [awaitingGateway, setAwaitingGateway] = useState(searchParams.get("payment") === "returned");
  const pollInterval = awaitingGateway ? 2000 : false;

  const plansQuery = useQuery({ queryKey: ["subscriptionPlans"], queryFn: getSubscriptionPlans });
  const mineQuery = useQuery({ queryKey: ["mySubscription"], queryFn: getMySubscription, refetchInterval: pollInterval });
  const invoicesQuery = useQuery({
    queryKey: ["mySubscriptionInvoices"],
    queryFn: getMySubscriptionInvoices,
    refetchInterval: pollInterval,
  });

  // The webhook settles the invoice a beat after the gateway redirects back — once nothing is
  // Pending any more, it has landed.
  const hasPendingInvoice = invoicesQuery.data?.some((i) => i.status === "Pending");
  useEffect(() => {
    if (!awaitingGateway) return;
    if (hasPendingInvoice === false) {
      setAwaitingGateway(false);
      return;
    }
    const timer = setTimeout(() => setAwaitingGateway(false), GATEWAY_POLL_TIMEOUT_MS);
    return () => clearTimeout(timer);
  }, [awaitingGateway, hasPendingInvoice]);

  function invalidateAll() {
    void queryClient.invalidateQueries({ queryKey: ["mySubscription"] });
    void queryClient.invalidateQueries({ queryKey: ["mySubscriptionInvoices"] });
  }

  const subscribeMutation = useMutation({
    mutationFn: (planId: number) => subscribeToPlan(planId),
    onSuccess: () => {
      setError(null);
      invalidateAll();
    },
    onError: () => setError(isAr ? "تعذّر إنشاء الاشتراك." : "Could not start the subscription."),
  });

  const cancelMutation = useMutation({
    mutationFn: cancelSubscription,
    onSuccess: invalidateAll,
    onError: () => setError(isAr ? "تعذّر إلغاء الاشتراك." : "Could not cancel the subscription."),
  });

  const payMutation = useMutation({
    mutationFn: (invoiceId: string) => payInvoice(invoiceId),
    onSuccess: (result) => {
      setError(null);
      if (result.redirectUrl) {
        // Same tab, not a popup: success_url brings them straight back to this page.
        setAwaitingGateway(true);
        window.location.href = result.redirectUrl;
      } else {
        invalidateAll();
      }
    },
    onError: () => setError(isAr ? "تعذّر بدء الدفع." : "Could not start checkout."),
  });

  function refreshAfterGateway() {
    setAwaitingGateway(false);
    invalidateAll();
  }

  const mine = mineQuery.data;
  const pendingInvoice = invoicesQuery.data?.find((i) => i.status === "Pending");

  return (
    <AppShell>
      <h1 className="mb-1 font-display text-2xl font-bold">{isAr ? "الاشتراك" : "Subscription"}</h1>
      <p className="mb-6 text-sm text-ink-faint">
        {isAr
          ? "خطط مدفوعة تخفّض عمولة المنصة وتفتح عروض المزايدة العامة."
          : "Paid plans lower the platform commission and unlock broadcast bidding leads."}
      </p>

      {mine && (
        <Card className="mb-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-xs text-ink-faint">{isAr ? "خطتك الحالية" : "Your current plan"}</p>
              <p className="font-display text-lg font-bold">{isAr ? mine.effectivePlan.nameAr : mine.effectivePlan.nameEn}</p>
            </div>
            {mine.status && (
              <StatusTag
                status={mine.status}
                label={
                  (STATUS_LABEL[mine.status] ? (isAr ? STATUS_LABEL[mine.status].ar : STATUS_LABEL[mine.status].en) : mine.status) +
                  (mine.cancelAtPeriodEnd ? ` · ${isAr ? "لن تُجدَّد" : "won't renew"}` : "")
                }
              />
            )}
          </div>
          {mine.currentPeriodEndUtc && (
            <p className="mt-2 text-xs text-ink-faint">
              {isAr ? "الفترة الحالية حتى " : "Current period ends "}
              <Ltr className="font-mono">{formatDate(mine.currentPeriodEndUtc)}</Ltr>
            </p>
          )}
          {mine.status === "PastDue" && pendingInvoice && (
            <div className="mt-4 flex items-center gap-3 rounded-md border border-rubric-tint bg-rubric-tint/40 p-3">
              <TriangleAlert className="h-4 w-4 shrink-0 text-rubric" />
              <p className="flex-1 text-sm text-rubric">
                {isAr
                  ? `فاتورة بمبلغ ${formatCurrency(pendingInvoice.total)} بانتظار الدفع — استحقاقها `
                  : `An invoice for ${formatCurrency(pendingInvoice.total)} is awaiting payment — due `}
                <Ltr className="font-mono">{formatDate(pendingInvoice.dueAtUtc)}</Ltr>
                {isAr ? "، وإلا ستتوقف مزايا خطتك." : ", or your plan's entitlements will lapse."}
              </p>
            </div>
          )}
          {(mine.status === "Active" || mine.status === "PastDue") && !mine.cancelAtPeriodEnd && (
            <Button variant="ghost" className="mt-4" onClick={() => cancelMutation.mutate()} disabled={cancelMutation.isPending}>
              {isAr ? "إلغاء الاشتراك" : "Cancel subscription"}
            </Button>
          )}
        </Card>
      )}

      {awaitingGateway && (
        <Card className="mb-6">
          <p className="mb-3 text-sm text-ink-soft">
            {isAr
              ? "جارٍ تأكيد الدفع… قد يستغرق ذلك بضع ثوانٍ."
              : "Confirming your payment… this can take a few seconds."}
          </p>
          <Button variant="secondary" onClick={refreshAfterGateway}>
            {isAr ? "تحقّق من حالة الدفع" : "Check payment status"}
          </Button>
        </Card>
      )}

      {error && <p className="mb-4 text-sm text-rubric">{error}</p>}

      <div className="mb-8 grid grid-cols-1 gap-4 sm:grid-cols-3">
        {plansQuery.data?.map((plan) => {
          const isCurrent = mine?.effectivePlan.id === plan.id;
          return (
            <Card key={plan.id} className={isCurrent ? "border-seal" : undefined}>
              <div className="mb-3 flex items-center justify-between">
                <h2 className="font-display text-lg font-bold">{isAr ? plan.nameAr : plan.nameEn}</h2>
                {isCurrent && <Check className="h-4 w-4 text-seal" />}
              </div>
              <p className="mb-4 font-mono text-2xl font-bold text-ink">
                <Ltr>{plan.monthlyPrice === 0 ? (isAr ? "مجاني" : "Free") : formatCurrency(plan.monthlyPrice)}</Ltr>
                {plan.monthlyPrice > 0 && <span className="text-xs font-normal text-ink-faint"> / {isAr ? "شهريًا" : "mo"}</span>}
              </p>
              <p className="mb-4 text-sm text-ink-soft">{isAr ? plan.descriptionAr : plan.descriptionEn}</p>
              <ul className="mb-4 flex flex-col gap-2 text-xs text-ink-soft">
                <li className="flex items-center gap-2">
                  <Percent className="h-3.5 w-3.5 text-seal" />
                  {isAr
                    ? `عمولة المنصة: ${plan.commissionPercentageOverride ?? 15}%`
                    : `Platform commission: ${plan.commissionPercentageOverride ?? 15}%`}
                </li>
                <li className="flex items-center gap-2">
                  <Megaphone className="h-3.5 w-3.5 text-seal" />
                  {plan.includesBroadcastBidding
                    ? isAr ? "عروض مزايدة عامة" : "Broadcast bidding leads"
                    : isAr ? "عروض مُرسلة بالاسم فقط" : "Targeted requests only"}
                </li>
              </ul>
              {!isCurrent && plan.monthlyPrice > 0 && (
                <Button
                  className="w-full justify-center"
                  onClick={() => subscribeMutation.mutate(plan.id)}
                  disabled={subscribeMutation.isPending || mine?.status === "PastDue" || mine?.status === "Active"}
                >
                  {isAr ? "الترقية إلى هذه الخطة" : "Upgrade to this plan"}
                </Button>
              )}
            </Card>
          );
        })}
      </div>

      <h2 className="mb-3 text-sm font-semibold text-ink-soft">{isAr ? "الفواتير" : "Invoices"}</h2>
      <div className="flex flex-col gap-2">
        {invoicesQuery.data?.map((invoice) => (
          <Card key={invoice.id} className="flex items-center justify-between gap-4">
            <div>
              <p className="font-mono text-sm font-medium text-ink">{invoice.number}</p>
              <p className="text-xs text-ink-faint">
                <Ltr>{formatDate(invoice.periodStartUtc)}</Ltr> – <Ltr>{formatDate(invoice.periodEndUtc)}</Ltr>
              </p>
            </div>
            <div className="flex items-center gap-3">
              <span className="font-mono text-sm text-ink-soft">
                <Ltr>{formatCurrency(invoice.total)}</Ltr>
              </span>
              <StatusTag
                status={invoice.status}
                label={INVOICE_STATUS_LABEL[invoice.status] ? (isAr ? INVOICE_STATUS_LABEL[invoice.status].ar : INVOICE_STATUS_LABEL[invoice.status].en) : invoice.status}
              />
              {invoice.status === "Pending" && (
                <Button onClick={() => payMutation.mutate(invoice.id)} disabled={payMutation.isPending}>
                  <CreditCard className="h-4 w-4" />
                  {isAr ? "ادفع الآن" : "Pay now"}
                </Button>
              )}
            </div>
          </Card>
        ))}
        {invoicesQuery.data?.length === 0 && (
          <p className="text-sm text-ink-faint">{isAr ? "لا توجد فواتير حتى الآن." : "No invoices yet."}</p>
        )}
      </div>
    </AppShell>
  );
}
