import { useState, type FormEvent } from "react";
import { useMutation } from "@tanstack/react-query";
import { Ban, CircleCheck } from "lucide-react";
import { Button } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { setSuspended, type AccountStatus } from "../../lib/directoryApi";

/** Suspend (or re-activate) with a required reason; it's written to the audit log. */
export function SuspendAccount({ userId, status, onDone }: { userId: string; status: AccountStatus; onDone: () => void }) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [tried, setTried] = useState(false);
  const suspend = status !== "Suspended";
  const run = useMutation({
    mutationFn: () => setSuspended(userId, suspend, reason),
    onSuccess: () => { setOpen(false); setReason(""); setTried(false); onDone(); },
  });
  if (status === "Deleted") return null;
  const error = !tried ? null : !reason.trim() ? t("form.required") : reason.trim().length < 5 ? t("directory.reasonShort") : null;

  function submit(e: FormEvent) {
    e.preventDefault();
    setTried(true);
    if (reason.trim().length >= 5) run.mutate();
  }

  if (!open)
    return (
      <Button variant={suspend ? "danger" : "secondary"} onClick={() => setOpen(true)}>
        {suspend ? <Ban className="h-4 w-4" /> : <CircleCheck className="h-4 w-4" />}
        {suspend ? t("directory.suspend") : t("directory.reactivate")}
      </Button>
    );
  return (
    <form className="flex w-full flex-col gap-2 rounded-md border border-warning/40 bg-warning-tint p-3 sm:w-96" onSubmit={submit} noValidate>
      <span className="text-sm font-semibold text-ink">{suspend ? t("directory.suspendTitle") : t("directory.reactivateTitle")}</span>
      {suspend && <span className="text-xs text-ink-soft">{t("directory.suspendEffect")}</span>}
      <input id={`suspend-${userId}`} value={reason} onChange={(e) => setReason(e.target.value)} placeholder={t("directory.reason")} aria-invalid={!!error}
        className={(error ? "border-rubric " : "border-border ") + "rounded-md border bg-surface-raised px-3 py-2 text-sm text-ink"} />
      {error && <span className="text-xs text-rubric">{error}</span>}
      <div className="flex gap-2">
        <Button type="submit" variant={suspend ? "danger" : "primary"} disabled={run.isPending}>{t("directory.confirm")}</Button>
        <Button type="button" variant="ghost" onClick={() => { setOpen(false); setTried(false); }}>{t("directory.cancel")}</Button>
      </div>
      {run.isError && <span className="text-xs text-rubric">{t("directory.actionFailed")}</span>}
    </form>
  );
}
