import { useState, type FormEvent, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, ArrowRight, Banknote, Send, UserX } from "lucide-react";
import { Button, Card, Input, Ltr, SectionHeading } from "@law-portal/ui";
import { formatCurrency, formatDate, formatDateTime, useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { COLLECTION_PILL, getLawyerDebt, recordDebtRepayment, sendDebtReminder } from "../lib/lawyerDebtsApi";

function Row({ label, children }: { label: ReactNode; children: ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-3 border-b border-rule py-2.5 text-sm last:border-0">
      <span className="text-ink-faint">{label}</span>
      <span>{children}</span>
    </div>
  );
}

/** One lawyer's debt: balance, how it's being collected, recording a bank transfer, reminders, history. */
export default function LawyerDebt() {
  const { id } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const query = useQuery({ queryKey: ["lawyerDebt", id], queryFn: () => getLawyerDebt(id!), enabled: !!id });
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ["lawyerDebt", id] });
    void queryClient.invalidateQueries({ queryKey: ["lawyerDebts"] });
  };

  const [showPay, setShowPay] = useState(false);
  const [amount, setAmount] = useState("");
  const [reference, setReference] = useState("");
  const [note, setNote] = useState("");
  const [tried, setTried] = useState(false);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);

  const d = query.data;
  const balance = d?.summary.balance ?? 0;
  const amountNum = Number(amount);
  const amountError = !tried ? null : !amount.trim() ? t("form.required") : !(amountNum > 0 && amountNum <= balance) ? t("debts.amountInvalid", { amount: formatCurrency(balance) }) : null;
  const referenceError = tried && !reference.trim() ? t("form.required") : null;

  const pay = useMutation({
    mutationFn: () => recordDebtRepayment(id!, amountNum, reference, note),
    onSuccess: () => {
      setShowPay(false); setAmount(""); setReference(""); setNote(""); setTried(false);
      setMessage({ ok: true, text: t("debts.paymentRecorded") });
      refresh();
    },
    onError: () => setMessage({ ok: false, text: t("debts.paymentFailed") }),
  });
  const remind = useMutation({
    mutationFn: () => sendDebtReminder(id!),
    onSuccess: () => { setMessage({ ok: true, text: t("debts.reminderSent") }); refresh(); },
    onError: () => setMessage({ ok: false, text: t("debts.reminderFailed") }),
  });

  function submit(e: FormEvent) {
    e.preventDefault();
    setTried(true);
    if (!amount.trim() || !(amountNum > 0 && amountNum <= balance) || !reference.trim()) return;
    pay.mutate();
  }

  return (
    <AppShell>
      <Link to="/lawyer-debts" className="mb-5 inline-flex items-center gap-1.5 text-sm text-ink-faint hover:text-ink">
        {isAr ? <ArrowRight className="h-4 w-4" /> : <ArrowLeft className="h-4 w-4" />}
        {t("debts.back")}
      </Link>
      {d && (
        <div className="mx-auto flex max-w-2xl flex-col gap-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <SectionHeading level={2}>
              <Link to={`/lawyers/${d.summary.lawyerProfileId}`} className="hover:underline">{t("debts.debtOf", { name: d.summary.fullName })}</Link>
            </SectionHeading>
            <span className={"rounded-full px-3 py-1 text-xs " + COLLECTION_PILL[d.summary.collection]}>{t(`debts.collections.${d.summary.collection}`)}</span>
          </div>

          <Card>
            <Row label={t("debts.totalDebt")}><Ltr className="font-mono">{formatCurrency(d.totalDebt)}</Ltr></Row>
            <Row label={t("debts.totalCollected")}><Ltr className="font-mono text-success">{d.totalCollected > 0 ? "−" : ""}{formatCurrency(d.totalCollected)}</Ltr></Row>
            <Row label={<b className="text-ink">{t("debts.remaining")}</b>}>
              <Ltr className={"font-mono text-lg font-semibold " + (balance > 0 ? "text-rubric" : "text-success")}>{formatCurrency(balance)}</Ltr>
            </Row>
            {d.summary.heldPayoutsTotal > 0 && balance > 0 && (
              <p className="mt-2 text-xs text-ink-faint">{t("debts.willOffset", { amount: formatCurrency(d.summary.heldPayoutsTotal) })}</p>
            )}
            {d.summary.accountDeleted && (
              <p className="mt-3 flex gap-2 rounded-md border border-rubric/40 bg-rubric-tint px-3 py-2 text-sm text-ink-soft">
                <UserX className="mt-0.5 h-4 w-4 shrink-0 text-rubric" />
                {t("debts.leftNotice", { date: d.summary.deletedAtUtc ? formatDate(d.summary.deletedAtUtc) : "—" })}
              </p>
            )}
            {d.suspendedPayoutsCount > 0 && (
              <p className="mt-2 text-xs text-warning">{t("debts.suspended", { count: d.suspendedPayoutsCount, amount: formatCurrency(d.suspendedPayoutsTotal) })}</p>
            )}

            <div className="mt-4 grid gap-1 rounded-md border border-rule bg-paper px-3 py-2 text-sm">
              <span className="text-xs text-ink-faint">{t("debts.contact")}</span>
              {d.contactEmail || d.contactPhone ? (
                <span className="flex flex-wrap gap-x-4">
                  {d.contactEmail && <Ltr className="font-mono">{d.contactEmail}</Ltr>}
                  {d.contactPhone && <Ltr className="font-mono">{d.contactPhone}</Ltr>}
                </span>
              ) : (
                <span className="text-ink-faint">—</span>
              )}
              {d.summary.lastReminderAtUtc && (
                <span className="text-xs text-ink-faint">{t("debts.lastReminder", { date: formatDateTime(d.summary.lastReminderAtUtc) })}</span>
              )}
            </div>

            {balance > 0 && (
              <div className="mt-4 flex flex-wrap gap-2">
                <Button onClick={() => { setShowPay(true); setMessage(null); }}><Banknote className="h-4 w-4" />{t("debts.recordPayment")}</Button>
                <Button variant="ghost" disabled={remind.isPending || !d.contactEmail} onClick={() => { setMessage(null); remind.mutate(); }}>
                  <Send className="h-4 w-4" />{t("debts.remindNow")}
                </Button>
              </div>
            )}
            {message && <p className={"mt-2 text-sm " + (message.ok ? "text-success" : "text-rubric")}>{message.text}</p>}

            {showPay && (
              <form className="mt-4 flex flex-col gap-3 rounded-md border border-border bg-paper p-4" onSubmit={submit} noValidate>
                <label className="flex flex-col gap-1.5">
                  <span className="text-xs font-medium text-ink-soft">{t("debts.amount")} *</span>
                  <Input id="debt-amount" dir="ltr" inputMode="decimal" value={amount} className={amountError ? "border-rubric!" : undefined} onChange={(e) => setAmount(e.target.value)} />
                  {amountError ? <span className="text-xs text-rubric">{amountError}</span> : <span className="text-xs text-ink-faint">{t("debts.owes", { amount: formatCurrency(balance) })}</span>}
                </label>
                <label className="flex flex-col gap-1.5">
                  <span className="text-xs font-medium text-ink-soft">{t("debts.reference")} *</span>
                  <Input id="debt-reference" dir="ltr" value={reference} className={referenceError ? "border-rubric!" : undefined} onChange={(e) => setReference(e.target.value)} />
                  {referenceError && <span className="text-xs text-rubric">{referenceError}</span>}
                </label>
                <label className="flex flex-col gap-1.5">
                  <span className="text-xs font-medium text-ink-soft">{t("debts.note")}</span>
                  <Input id="debt-note" value={note} onChange={(e) => setNote(e.target.value)} />
                </label>
                <div className="flex gap-2">
                  <Button type="submit" disabled={pay.isPending}>{t("debts.savePayment")}</Button>
                  <Button type="button" variant="ghost" onClick={() => { setShowPay(false); setTried(false); }}>{t("debts.cancel")}</Button>
                </div>
              </form>
            )}
          </Card>

          <Card>
            <SectionHeading level={3} className="mb-3">{t("debts.history")}</SectionHeading>
            <ol className="flex flex-col">
              {d.entries.map((e) => (
                <li key={e.id} className="flex items-start justify-between gap-3 border-b border-rule py-2.5 text-sm last:border-0">
                  <span>
                    {t(`debts.kinds.${e.kind}`, e.kind)}
                    {e.paymentNumber && e.paymentId && (
                      <> · <Link to={`/payments/${e.paymentId}`} className="text-seal-strong hover:underline"><Ltr className="font-mono">{e.paymentNumber}</Ltr></Link></>
                    )}
                    {e.reference && e.kind === "BankTransfer" && <> · <Ltr className="font-mono text-ink-faint">{e.reference}</Ltr></>}
                    {e.note && e.kind === "BankTransfer" && <span className="block text-xs text-ink-faint">{e.note}</span>}
                    <span className="block text-xs text-ink-faint"><Ltr className="font-mono">{formatDateTime(e.createdAtUtc)}</Ltr></span>
                  </span>
                  <Ltr className={"font-mono " + (e.amount > 0 ? "text-rubric" : "text-success")}>{e.amount > 0 ? "+" : ""}{formatCurrency(e.amount)}</Ltr>
                </li>
              ))}
            </ol>
          </Card>
        </div>
      )}
    </AppShell>
  );
}
