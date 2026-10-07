import { useState, type FormEvent, type ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { CalendarClock, Check, CreditCard, ShieldCheck, Smartphone, Wallet as WalletIcon } from "lucide-react";
import { Button, Card, Chip, Input, Ltr } from "@law-portal/ui";
import { formatCurrency, useTranslation } from "@law-portal/i18n";
import { getWallet, previewCheckout } from "../lib/paymentsApi";

export type CheckoutMethod = "Card" | "Wallet";

/** The pay step of a request: method, discount code, the breakdown the server will charge, and the
 * "pay inside the platform" notice. Every figure comes from the preview endpoint, never computed here. */
export function CheckoutPanel({
  requestId,
  isConsultation,
  isInstant,
  pending,
  error,
  onCheckout,
}: {
  requestId: string;
  isConsultation: boolean;
  isInstant: boolean;
  pending: boolean;
  error: string | null;
  onCheckout: (method: CheckoutMethod, discountCode: string | null) => void;
}) {
  const { t } = useTranslation();
  const [method, setMethod] = useState<CheckoutMethod>("Card");
  const [codeInput, setCodeInput] = useState("");
  const [appliedCode, setAppliedCode] = useState<string | null>(null);

  const preview = useQuery({
    queryKey: ["checkoutPreview", requestId, appliedCode],
    queryFn: () => previewCheckout(requestId, appliedCode),
    placeholderData: (previous) => previous,
  });
  const wallet = useQuery({ queryKey: ["wallet"], queryFn: getWallet });

  const rejection = appliedCode ? preview.data?.rejection : null;
  const codeAccepted = !!appliedCode && !preview.isFetching && preview.data?.rejection === null;
  const due = preview.data?.total;
  const walletShort = method === "Wallet" && wallet.data != null && due != null && wallet.data.balance < due;

  function apply(e: FormEvent) {
    e.preventDefault();
    const code = codeInput.trim();
    if (code) setAppliedCode(code.toUpperCase());
  }

  function removeCode() {
    setAppliedCode(null);
    setCodeInput("");
  }

  return (
    <Card className="mb-4 flex flex-col gap-5">
      <section>
        <h2 className="mb-3 text-sm font-semibold text-ink-soft">{t("checkout.method")}</h2>
        <div className="grid gap-2 sm:grid-cols-2">
          <MethodOption
            selected={method === "Card"}
            onSelect={() => setMethod("Card")}
            icon={<CreditCard className="h-4 w-4" />}
            title={t("checkout.card")}
            hint={t("checkout.cardHint")}
          />
          <MethodOption
            selected={method === "Wallet"}
            onSelect={() => setMethod("Wallet")}
            icon={<WalletIcon className="h-4 w-4" />}
            title={t("checkout.wallet")}
            hint={
              wallet.data && (
                <>
                  {t("checkout.walletBalance")}{" "}
                  <Ltr className="font-mono">{formatCurrency(wallet.data.balance)}</Ltr>
                </>
              )
            }
          />
          <MethodOption disabled icon={<Smartphone className="h-4 w-4" />} title={t("checkout.applePay")} />
          <MethodOption disabled icon={<CalendarClock className="h-4 w-4" />} title={t("checkout.tamara")} hint={t("checkout.tamaraHint")} />
        </div>
      </section>

      <section>
        <h2 className="mb-2 text-sm font-semibold text-ink-soft">{t("discount.question")}</h2>
        {codeAccepted ? (
          <div className="flex items-center justify-between rounded-md border border-seal bg-seal-tint px-4 py-2.5 text-sm text-seal-strong">
            <span className="flex items-center gap-2">
              <Check className="h-4 w-4" />
              {t("discount.applied")} <Ltr className="font-mono">{appliedCode}</Ltr>
            </span>
            <button type="button" onClick={removeCode} className="text-xs font-medium hover:underline">
              {t("discount.remove")}
            </button>
          </div>
        ) : (
          <form className="flex gap-2" onSubmit={apply}>
            <Input
              className="flex-1"
              dir="ltr"
              value={codeInput}
              onChange={(e) => setCodeInput(e.target.value)}
              placeholder={t("discount.placeholder")}
              autoComplete="off"
            />
            <Button type="submit" variant="secondary" disabled={!codeInput.trim() || preview.isFetching}>
              {t("discount.apply")}
            </Button>
          </form>
        )}
        {rejection && <p className="mt-2 text-sm text-rubric">{t(`discount.reasons.${rejection}`)}</p>}
      </section>

      {preview.data && (
        <section className="rounded-md border border-border bg-paper px-4">
          <Line label={t(isConsultation ? "checkout.priceConsultation" : "checkout.priceService")} amount={preview.data.gross} />
          {preview.data.discount > 0 && <Line label={t("checkout.discount")} amount={-preview.data.discount} accent />}
          {preview.data.vatAmount > 0 && <Line label={t("checkout.vat")} amount={preview.data.vatAmount} />}
          <Line label={t("checkout.total")} amount={preview.data.total} strong />
        </section>
      )}

      <p className="flex items-start gap-2 text-xs text-ink-soft">
        <ShieldCheck className="mt-0.5 h-4 w-4 shrink-0 text-seal" />
        {t("checkout.notice")}
      </p>
      {isInstant && <p className="text-xs text-ink-faint">{t("checkout.instantRedirect")}</p>}

      {(error || walletShort) && <p className="text-sm text-rubric">{walletShort ? t("checkout.insufficientWallet") : error}</p>}
      <Button
        className="w-full justify-center"
        disabled={pending || !preview.data || preview.isFetching || walletShort}
        onClick={() => onCheckout(method, codeAccepted ? appliedCode : null)}
      >
        {t("checkout.submit")}
      </Button>
    </Card>
  );
}

function MethodOption({
  selected = false,
  disabled = false,
  onSelect,
  icon,
  title,
  hint,
}: {
  selected?: boolean;
  disabled?: boolean;
  onSelect?: () => void;
  icon: ReactNode;
  title: string;
  hint?: ReactNode;
}) {
  const { t } = useTranslation();
  return (
    <button
      type="button"
      role="radio"
      aria-checked={selected}
      disabled={disabled}
      onClick={onSelect}
      className={
        "flex items-center justify-between rounded-md border px-4 py-3 text-start text-sm transition-colors " +
        (disabled
          ? "cursor-not-allowed border-border opacity-60"
          : selected
            ? "border-seal bg-seal-tint text-seal-strong"
            : "border-border hover:border-seal")
      }
    >
      <span className="flex items-center gap-3">
        {icon}
        <span>
          <span className="block font-medium">{title}</span>
          {hint && <span className="block text-xs text-ink-faint">{hint}</span>}
        </span>
      </span>
      {disabled ? <Chip>{t("checkout.soon")}</Chip> : selected && <Check className="h-4 w-4" />}
    </button>
  );
}

function Line({ label, amount, strong, accent }: { label: string; amount: number; strong?: boolean; accent?: boolean }) {
  return (
    <div
      className={
        "flex items-center justify-between border-b border-border py-2.5 text-sm last:border-0 " +
        (strong ? "font-semibold text-ink" : "text-ink-soft")
      }
    >
      <span>{label}</span>
      <Ltr className={"font-mono " + (accent ? "text-seal" : "")}>{formatCurrency(amount)}</Ltr>
    </div>
  );
}
