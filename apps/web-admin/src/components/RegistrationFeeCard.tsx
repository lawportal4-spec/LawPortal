import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button, Card, Input, Ltr, SectionHeading } from "@law-portal/ui";
import { formatCurrency, useTranslation } from "@law-portal/i18n";
import { getRegistrationFeeSetting, updateRegistrationFeeSetting } from "../lib/subscriptionsApi";

/** The one-off fee approved lawyers pay before their account opens. */
export function RegistrationFeeCard() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const query = useQuery({ queryKey: ["registrationFeeSetting"], queryFn: getRegistrationFeeSetting });
  const [amount, setAmount] = useState("");
  const [isEnabled, setIsEnabled] = useState(true);
  const [notice, setNotice] = useState<{ ok: boolean; text: string } | null>(null);

  useEffect(() => {
    if (!query.data) return;
    setAmount(String(query.data.amount));
    setIsEnabled(query.data.isEnabled);
  }, [query.data]);

  const save = useMutation({
    mutationFn: () => updateRegistrationFeeSetting(Number(amount), isEnabled),
    onSuccess: (data) => {
      queryClient.setQueryData(["registrationFeeSetting"], data);
      setNotice({ ok: true, text: t("lawyerOnboarding.admin.saved") });
    },
    onError: () => setNotice({ ok: false, text: t("lawyerOnboarding.admin.saveFailed") }),
  });

  const value = Number(amount);
  const total = Math.round(value * 1.15 * 100) / 100;

  return (
    <Card className="mb-8">
      <SectionHeading level={2} className="mb-1">
        {t("lawyerOnboarding.admin.title")}
      </SectionHeading>
      <p className="mb-4 text-sm text-ink-faint">{t("lawyerOnboarding.admin.hint")}</p>
      <div className="flex flex-wrap items-end gap-4">
        <label className="flex flex-col gap-1 text-xs text-ink-faint">
          {t("lawyerOnboarding.admin.amount")}
          <Input dir="ltr" type="number" min="1" step="0.01" className="w-40" value={amount} onChange={(e) => setAmount(e.target.value)} />
        </label>
        <label className="flex items-center gap-2 pb-3 text-sm">
          <input type="checkbox" checked={isEnabled} onChange={(e) => setIsEnabled(e.target.checked)} />
          {t("lawyerOnboarding.admin.enabled")}
        </label>
        {value > 0 && (
          <p className="pb-3 text-sm text-ink-soft">
            {t("lawyerOnboarding.admin.total")}: <Ltr className="font-mono font-semibold text-ink">{formatCurrency(total)}</Ltr>
          </p>
        )}
        <Button className="mb-1" disabled={!(value >= 1) || save.isPending} onClick={() => save.mutate()}>
          {t("lawyerOnboarding.admin.save")}
        </Button>
      </div>
      {!isEnabled && <p className="mt-3 text-xs text-warning">{t("lawyerOnboarding.admin.disabledNote")}</p>}
      {notice && <p className={"mt-2 text-sm " + (notice.ok ? "text-seal" : "text-rubric")}>{notice.text}</p>}
    </Card>
  );
}
