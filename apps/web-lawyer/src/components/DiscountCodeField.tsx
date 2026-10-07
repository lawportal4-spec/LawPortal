import { useEffect, useState, type FormEvent } from "react";
import { useQuery } from "@tanstack/react-query";
import { Check } from "lucide-react";
import { Button, Input, Ltr } from "@law-portal/ui";
import { formatCurrency, useTranslation } from "@law-portal/i18n";
import { previewCheckout } from "../lib/subscriptionApi";

/** "Have a discount code?" for a lawyer payment. Reports the accepted code (or null) upward and
 * shows what the server will charge with it. */
export function DiscountCodeField({
  referenceId,
  onChange,
  className,
  showTotals = true,
}: {
  referenceId: string;
  onChange: (code: string | null) => void;
  className?: string;
  /** Off when the page already shows its own breakdown. */
  showTotals?: boolean;
}) {
  const { t } = useTranslation();
  const [input, setInput] = useState("");
  const [applied, setApplied] = useState<string | null>(null);

  const preview = useQuery({
    queryKey: ["checkoutPreview", referenceId, applied],
    queryFn: () => previewCheckout(referenceId, applied),
    enabled: applied !== null,
  });

  function apply(e: FormEvent) {
    e.preventDefault();
    if (input.trim()) setApplied(input.trim().toUpperCase());
  }

  function remove() {
    setApplied(null);
    setInput("");
  }

  const accepted = applied && preview.data && preview.data.rejection === null && !preview.isFetching;
  const acceptedCode = accepted ? applied : null;
  useEffect(() => onChange(acceptedCode), [acceptedCode, onChange]);

  return (
    <div className={className}>
      {accepted ? (
        <div className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-seal bg-seal-tint px-4 py-2.5 text-sm text-seal-strong">
          <span className="flex items-center gap-2">
            <Check className="h-4 w-4" />
            {t("discount.applied")} <Ltr className="font-mono">{applied}</Ltr>
          </span>
          <span className="flex items-center gap-3">
            {showTotals && (
              <>
                <span>
                  {t("checkout.discount")} <Ltr className="font-mono">{formatCurrency(-preview.data.discount)}</Ltr>
                </span>
                <span className="font-semibold">
                  {t("checkout.total")} <Ltr className="font-mono">{formatCurrency(preview.data.total)}</Ltr>
                </span>
              </>
            )}
            <button type="button" onClick={remove} className="text-xs font-medium hover:underline">
              {t("discount.remove")}
            </button>
          </span>
        </div>
      ) : (
        <form className="flex gap-2" onSubmit={apply}>
          <Input
            className="flex-1"
            dir="ltr"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder={t("discount.placeholder")}
            autoComplete="off"
          />
          <Button type="submit" variant="secondary" disabled={!input.trim() || preview.isFetching}>
            {t("discount.apply")}
          </Button>
        </form>
      )}
      {applied && preview.data?.rejection && (
        <p className="mt-2 text-sm text-rubric">{t(`discount.reasons.${preview.data.rejection}`)}</p>
      )}
    </div>
  );
}
