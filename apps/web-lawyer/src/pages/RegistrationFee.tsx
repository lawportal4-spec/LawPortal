import { useEffect, useState, type ReactNode } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Navigate, useNavigate, useSearchParams } from "react-router-dom";
import { CalendarClock, Check, CreditCard, Info, Smartphone } from "lucide-react";
import { Button, Card, Chip, Ltr } from "@law-portal/ui";
import { formatCurrency, useTranslation } from "@law-portal/i18n";
import { AuthLayout } from "../components/AuthForm";
import { DiscountCodeField } from "../components/DiscountCodeField";
import { getLawyerMe } from "../lib/authApi";
import { getRegistrationFee, payRegistrationFee, previewCheckout } from "../lib/subscriptionApi";

/** Same bound as the other gateway returns: stop polling for the webhook after this long. */
const GATEWAY_POLL_TIMEOUT_MS = 90_000;

/** The registration-fee checkout an approved lawyer passes before the portal opens. */
export default function RegistrationFee() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [awaitingGateway, setAwaitingGateway] = useState(params.get("payment") === "returned");
  const [discountCode, setDiscountCode] = useState<string | null>(null);

  const me = useQuery({
    queryKey: ["lawyerMe"],
    queryFn: getLawyerMe,
    gcTime: 0,
    refetchInterval: (q) => (awaitingGateway && !q.state.data?.isApproved ? 2000 : false),
  });
  const isApproved = me.data?.isApproved ?? false;
  const fee = useQuery({ queryKey: ["registrationFee"], queryFn: getRegistrationFee, enabled: me.data?.onboardingStep === "PayFee" });
  const preview = useQuery({
    queryKey: ["checkoutPreview", fee.data?.invoiceId, discountCode],
    queryFn: () => previewCheckout(fee.data!.invoiceId, discountCode),
    enabled: !!fee.data && !!discountCode,
  });

  const pay = useMutation({
    mutationFn: () => payRegistrationFee(discountCode),
    onSuccess: (result) => {
      // Fully discounted: settled on the spot — the refreshed status opens the portal.
      if (result.paidImmediately) return void me.refetch();
      if (!result.redirectUrl) return;
      setAwaitingGateway(true);
      window.location.href = result.redirectUrl;
    },
  });

  useEffect(() => {
    if (isApproved) {
      // Paid: the portal is open — straight to completing the profile ("ابدأ بتسجيل بياناتك").
      const timer = setTimeout(() => navigate("/settings", { replace: true }), 1500);
      return () => clearTimeout(timer);
    }
    if (!awaitingGateway) return;
    const timer = setTimeout(() => setAwaitingGateway(false), GATEWAY_POLL_TIMEOUT_MS);
    return () => clearTimeout(timer);
  }, [isApproved, awaitingGateway, navigate]);

  if (me.data && !isApproved && me.data.onboardingStep !== "PayFee") return <Navigate to="/" replace />;

  const lines = discountCode && preview.data?.rejection === null ? preview.data : null;
  const base = fee.data?.baseAmount ?? 0;
  const vat = lines?.vatAmount ?? fee.data?.vatAmount ?? 0;
  const total = lines?.total ?? fee.data?.total ?? 0;

  return (
    <AuthLayout hideNav title={t("lawyerOnboarding.feeTitle")}>
      {isApproved ? (
        <p className="text-center text-sm font-medium text-seal">{t("lawyerOnboarding.paidRedirect")}</p>
      ) : awaitingGateway ? (
        <Card className="flex flex-col gap-3 text-center">
          <p className="text-sm text-ink-soft">{t("lawyerOnboarding.confirming")}</p>
          <Button variant="secondary" onClick={() => void me.refetch()}>
            {t("lawyerOnboarding.checkStatus")}
          </Button>
        </Card>
      ) : (
        <div className="flex flex-col gap-5">
          <p className="flex items-start gap-2 rounded-md border border-seal bg-seal-tint px-4 py-3 text-sm text-seal-strong">
            <Info className="mt-0.5 h-4 w-4 shrink-0" />
            {t("lawyerOnboarding.feeBanner")}
          </p>

          <section>
            <h2 className="mb-2 text-sm font-semibold text-ink-soft">{t("checkout.method")}</h2>
            <div className="flex flex-col gap-2">
              <Method selected icon={<CreditCard className="h-4 w-4" />} title={t("checkout.card")} hint={t("checkout.cardHint")} />
              <Method icon={<Smartphone className="h-4 w-4" />} title={t("checkout.applePay")} />
              <Method icon={<CalendarClock className="h-4 w-4" />} title={t("checkout.tamara")} hint={t("checkout.tamaraHint")} />
            </div>
          </section>

          {fee.data && (
            <section>
              <h2 className="mb-2 text-sm font-semibold text-ink-soft">{t("discount.question")}</h2>
              <DiscountCodeField referenceId={fee.data.invoiceId} onChange={setDiscountCode} showTotals={false} />
            </section>
          )}

          {fee.data && (
            <section className="rounded-md border border-border bg-paper px-4">
              <Line label={t("lawyerOnboarding.feeAmount")} amount={base} />
              {lines && lines.discount > 0 && <Line label={t("checkout.discount")} amount={-lines.discount} accent />}
              <Line label={t("lawyerOnboarding.vat")} amount={vat} />
              <Line label={t("lawyerOnboarding.total")} amount={total} strong />
            </section>
          )}

          {(pay.isError || fee.isError) && <p className="text-sm text-rubric">{t("lawyerOnboarding.payFailed")}</p>}
          <Button className="w-full justify-center" disabled={!fee.data || pay.isPending || preview.isFetching} onClick={() => pay.mutate()}>
            {t("lawyerOnboarding.pay")}
          </Button>
        </div>
      )}
    </AuthLayout>
  );
}

/** Card is the only live method; Apple Pay and Tamara are shown as coming soon. */
function Method({ selected = false, icon, title, hint }: { selected?: boolean; icon: ReactNode; title: string; hint?: string }) {
  const { t } = useTranslation();
  return (
    <div
      aria-disabled={!selected}
      className={
        "flex items-center justify-between rounded-md border px-4 py-3 text-sm " +
        (selected ? "border-seal bg-seal-tint text-seal-strong" : "border-border opacity-60")
      }
    >
      <span className="flex items-center gap-3">
        {icon}
        <span>
          <span className="block font-medium">{title}</span>
          {hint && <span className="block text-xs text-ink-faint">{hint}</span>}
        </span>
      </span>
      {selected ? <Check className="h-4 w-4" /> : <Chip>{t("checkout.soon")}</Chip>}
    </div>
  );
}

function Line({ label, amount, strong, accent }: { label: string; amount: number; strong?: boolean; accent?: boolean }) {
  return (
    <div className={"flex items-center justify-between border-b border-border py-2.5 text-sm last:border-0 " + (strong ? "font-semibold text-ink" : "text-ink-soft")}>
      <span>{label}</span>
      <Ltr className={"font-mono " + (accent ? "text-seal" : "")}>{formatCurrency(amount)}</Ltr>
    </div>
  );
}
