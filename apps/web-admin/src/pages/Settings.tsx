import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button, Card, Input, SectionHeading } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getRefundPolicy, saveRefundPolicy } from "../lib/lawyerDebtsApi";

/** Platform settings: the refund period after a lawyer is paid, and debt reminders. */
export default function Settings() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const policy = useQuery({ queryKey: ["refundPolicy"], queryFn: getRefundPolicy });
  const [windowDays, setWindowDays] = useState("");
  const [reminderDays, setReminderDays] = useState("");
  const [tried, setTried] = useState(false);
  const [saved, setSaved] = useState<boolean | null>(null);

  useEffect(() => {
    if (policy.data) {
      setWindowDays(String(policy.data.refundWindowDays));
      setReminderDays(String(policy.data.debtReminderIntervalDays));
    }
  }, [policy.data]);

  const inRange = (v: string, max: number) => /^\d+$/.test(v.trim()) && Number(v) >= 1 && Number(v) <= max;
  const windowError = !tried ? null : !windowDays.trim() ? t("form.required") : inRange(windowDays, 365) ? null : t("settings.range", { max: 365 });
  const reminderError = !tried ? null : !reminderDays.trim() ? t("form.required") : inRange(reminderDays, 90) ? null : t("settings.range", { max: 90 });

  const save = useMutation({
    mutationFn: () => saveRefundPolicy({ refundWindowDays: Number(windowDays), debtReminderIntervalDays: Number(reminderDays) }),
    onSuccess: () => { setSaved(true); void queryClient.invalidateQueries({ queryKey: ["refundPolicy"] }); },
    onError: () => setSaved(false),
  });

  function submit(e: FormEvent) {
    e.preventDefault();
    setTried(true);
    setSaved(null);
    if (inRange(windowDays, 365) && inRange(reminderDays, 90)) save.mutate();
  }

  return (
    <AppShell>
      <SectionHeading level={2} className="mb-5">{t("settings.title")}</SectionHeading>
      <Card className="max-w-2xl">
        <SectionHeading level={3} className="mb-1">{t("settings.refunds")}</SectionHeading>
        <p className="mb-4 text-xs text-ink-faint">{t("settings.refundsHint")}</p>
        <form className="flex flex-col gap-4" onSubmit={submit} noValidate>
          <label className="grid items-start gap-3 sm:grid-cols-[1fr_9rem]">
            <span className="flex flex-col text-sm">
              {t("settings.windowDays")}
              <span className="text-xs text-ink-faint">{t("settings.windowHint")}</span>
            </span>
            <span className="flex flex-col gap-1">
              <Input id="refund-window" dir="ltr" inputMode="numeric" value={windowDays} className={windowError ? "border-rubric!" : undefined} onChange={(e) => setWindowDays(e.target.value)} />
              {windowError && <span className="text-xs text-rubric">{windowError}</span>}
            </span>
          </label>
          <label className="grid items-start gap-3 sm:grid-cols-[1fr_9rem]">
            <span className="flex flex-col text-sm">
              {t("settings.reminderDays")}
              <span className="text-xs text-ink-faint">{t("settings.reminderHint")}</span>
            </span>
            <span className="flex flex-col gap-1">
              <Input id="reminder-days" dir="ltr" inputMode="numeric" value={reminderDays} className={reminderError ? "border-rubric!" : undefined} onChange={(e) => setReminderDays(e.target.value)} />
              {reminderError && <span className="text-xs text-rubric">{reminderError}</span>}
            </span>
          </label>
          <div className="flex items-center gap-3">
            <Button type="submit" disabled={save.isPending}>{t("settings.save")}</Button>
            {saved === true && <span className="text-sm text-success">{t("settings.saved")}</span>}
            {saved === false && <span className="text-sm text-rubric">{t("settings.failed")}</span>}
          </div>
        </form>
      </Card>
    </AppShell>
  );
}
