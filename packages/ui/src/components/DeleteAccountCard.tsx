import { useState, type FormEvent } from "react";
import { formatCurrency, useTranslation } from "@law-portal/i18n";
import { Button } from "./Button";
import { Card } from "./Card";
import { Input } from "./Input";
import { Ltr } from "./Ltr";
import { SectionHeading } from "./SectionHeading";

export interface DeletionImpact {
  userType: string;
  heldPayoutsTotal: number;
  heldPayoutsCount: number;
  debtBalance: number;
  walletBalance: number;
  openRequests: number;
}

/** "حذف الحساب" for clients and lawyers. Deleting is never blocked: the person first sees what they
 * lose (held shares, wallet balance) or still owe, then types a confirmation word. */
export function DeleteAccountCard({
  loadImpact,
  onDelete,
}: {
  loadImpact: () => Promise<DeletionImpact>;
  onDelete: () => Promise<void>;
}) {
  const { t } = useTranslation();
  const [impact, setImpact] = useState<DeletionImpact | null>(null);
  const [open, setOpen] = useState(false);
  const [word, setWord] = useState("");
  const [tried, setTried] = useState(false);
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);
  const confirmWord = t("deleteAccount.confirmWord");

  async function start() {
    setOpen(true);
    setImpact(await loadImpact().catch(() => null));
  }

  async function submit(e: FormEvent) {
    e.preventDefault();
    setTried(true);
    if (word.trim() !== confirmWord) return;
    setBusy(true);
    setFailed(false);
    try {
      await onDelete();
    } catch {
      setFailed(true);
      setBusy(false);
    }
  }

  const warnings: string[] = [];
  if (impact) {
    if (impact.heldPayoutsTotal > 0)
      warnings.push(t("deleteAccount.heldPayouts", { amount: formatCurrency(impact.heldPayoutsTotal), count: impact.heldPayoutsCount }));
    if (impact.debtBalance > 0) warnings.push(t("deleteAccount.debt", { amount: formatCurrency(impact.debtBalance) }));
    if (impact.walletBalance > 0) warnings.push(t("deleteAccount.wallet", { amount: formatCurrency(impact.walletBalance) }));
    if (impact.openRequests > 0 && impact.heldPayoutsCount === 0) warnings.push(t("deleteAccount.openRequests", { count: impact.openRequests }));
  }
  const wordError = tried && word.trim() !== confirmWord ? t("deleteAccount.wordMismatch", { word: confirmWord }) : null;

  return (
    <Card className="border-rubric/40">
      <SectionHeading level={3} className="mb-2 text-rubric">
        {t("deleteAccount.title")}
      </SectionHeading>
      <p className="text-sm text-ink-soft">{t("deleteAccount.intro")}</p>

      {!open ? (
        <Button variant="danger" className="mt-4" onClick={start}>
          {t("deleteAccount.start")}
        </Button>
      ) : (
        <form className="mt-4 flex flex-col gap-3" onSubmit={submit} noValidate>
          {impact === null ? (
            <p className="text-sm text-ink-faint">{t("deleteAccount.checking")}</p>
          ) : warnings.length > 0 ? (
            <ul className="flex flex-col gap-2 rounded-md border border-warning/40 bg-warning-tint p-3 text-sm text-ink-soft">
              {warnings.map((w) => (
                <li key={w} className="flex gap-2">
                  <span className="text-warning">•</span>
                  {w}
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-ink-soft">{t("deleteAccount.nothingPending")}</p>
          )}
          <label className="flex flex-col gap-1.5">
            <span className="text-xs font-medium text-ink-soft">
              {t("deleteAccount.typeToConfirm")} <Ltr className="font-mono text-rubric">{confirmWord}</Ltr>
            </span>
            <Input id="delete-confirm" value={word} autoComplete="off" aria-invalid={!!wordError}
              className={wordError ? "border-rubric!" : undefined} onChange={(e) => setWord(e.target.value)} />
            {wordError && <span className="text-xs text-rubric">{wordError}</span>}
          </label>
          <div className="flex flex-wrap items-center gap-2">
            <Button type="submit" variant="danger" disabled={busy || impact === null}>
              {t("deleteAccount.confirm")}
            </Button>
            <Button type="button" variant="ghost" onClick={() => { setOpen(false); setWord(""); setTried(false); }}>
              {t("deleteAccount.cancel")}
            </Button>
          </div>
          {failed && <p className="text-sm text-rubric">{t("deleteAccount.failed")}</p>}
        </form>
      )}
    </Card>
  );
}
