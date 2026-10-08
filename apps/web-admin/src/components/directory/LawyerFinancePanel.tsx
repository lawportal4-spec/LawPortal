import { Link } from "react-router-dom";
import { Scale } from "lucide-react";
import { Card, Ltr } from "@law-portal/ui";
import { formatCurrency, formatDateTime, useTranslation } from "@law-portal/i18n";
import type { LawyerFinance } from "../../lib/directoryApi";
import { Money, Stat } from "./bits";

/** Whether the platform owes the lawyer (he is دائن) or he owes it (مدين), the totals behind it,
 * a six-month chart, and the statement that explains the balance line by line. */
export function LawyerFinanceBalance({ f, onWhy }: { f: LawyerFinance; onWhy: () => void }) {
  const { t } = useTranslation();
  const b = f.totals.balance;
  const credit = b >= 0;
  // LRI…PDI isolates keep "2,498.08 SAR" from flipping inside the Arabic sentence.
  const money = (v: number) => `\u2066${formatCurrency(v)}\u2069`;
  return (
    <>
      <div className={"mb-4 flex flex-wrap items-center gap-x-6 gap-y-2 rounded-xl border px-4 py-3 " + (credit ? "border-success/40 bg-success-tint" : "border-rubric/50 bg-rubric-tint")}>
        <Scale className={"h-7 w-7 " + (credit ? "text-success" : "text-rubric")} />
        <div className="min-w-[220px] flex-1">
          <p className="font-display text-base font-bold">
            {b === 0 ? t("lawyerFinance.settled") : credit ? t("lawyerFinance.platformOwes") : t("lawyerFinance.lawyerOwes")}
            {b !== 0 && (
              <span className={"ms-2 inline-block rounded-full px-2.5 py-0.5 text-xs " + (credit ? "bg-success/20 text-success" : "bg-rubric/20 text-rubric")}>
                {credit ? t("lawyerFinance.creditor") : t("lawyerFinance.debtor")}
              </span>
            )}
          </p>
          <p className="text-sm text-ink-soft">
            {credit ? t("lawyerFinance.whyCredit", { held: money(f.totals.held + f.totals.suspended), debt: money(f.totals.debt) })
              : t("lawyerFinance.whyDebit", { debt: money(f.totals.debt), held: money(f.totals.held + f.totals.suspended) })}{" "}
            <button type="button" onClick={onWhy} className="text-seal-strong hover:underline">{t("lawyerFinance.openStatement")}</button>
          </p>
        </div>
        <Money value={b} className={"text-3xl font-semibold " + (credit ? "text-success" : "text-rubric")} />
      </div>
      <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <Stat label={t("lawyerFinance.earned")}><Money value={f.totals.earned} /></Stat>
        <Stat label={t("lawyerFinance.paidOut")}><Money value={f.totals.paidOut} className="text-success" /></Stat>
        <Stat label={t("lawyerFinance.held")}><Money value={f.totals.held + f.totals.suspended} className="text-seal-strong" /></Stat>
        <Stat label={t("lawyerFinance.debt")}><Money value={f.totals.debt} className={f.totals.debt > 0 ? "text-rubric" : ""} /></Stat>
      </div>
    </>
  );
}

export function LawyerEarningsChart({ f }: { f: LawyerFinance }) {
  const { t } = useTranslation();
  const max = Math.max(1, ...f.months.map((m) => m.paidOut + m.held + m.deducted));
  const H = 150, top = 20, base = top + H, step = 90, w = 34;
  const ticks = [0, 0.5, 1].map((k) => k * max);
  const monthName = (m: { year: number; month: number }) => `${String(m.month).padStart(2, "0")}/${m.year}`;
  return (
    <Card className="mb-4">
      <h2 className="mb-2 font-display text-base font-bold">{t("lawyerFinance.monthly")}</h2>
      <div className="mb-2 flex flex-wrap gap-4 text-xs text-ink-soft">
        <span><i className="me-1.5 inline-block h-2.5 w-2.5 rounded-sm bg-success align-middle" />{t("lawyerFinance.paidOut")}</span>
        <span><i className="me-1.5 inline-block h-2.5 w-2.5 rounded-sm bg-seal align-middle" />{t("lawyerFinance.held")}</span>
        <span><i className="me-1.5 inline-block h-2.5 w-2.5 rounded-sm bg-rubric align-middle" />{t("lawyerFinance.deducted")}</span>
      </div>
      <svg viewBox={`0 0 ${70 + step * f.months.length} ${base + 30}`} className="h-auto w-full" role="img" aria-label={t("lawyerFinance.monthly")}>
        {ticks.map((v) => {
          const y = base - (v / max) * H;
          return (
            <g key={v}>
              <line x1="60" x2={60 + step * f.months.length} y1={y} y2={y} stroke="var(--color-rule)" strokeDasharray={v ? "3 4" : undefined} />
              <text x="54" y={y + 4} textAnchor="end" fontSize="11" fill="var(--color-ink-faint)" fontFamily="var(--font-mono)">{Math.round(v)}</text>
            </g>
          );
        })}
        {f.months.map((m, i) => {
          const x = 75 + i * step;
          const hp = (m.paidOut / max) * H, hh = (m.held / max) * H, hd = (m.deducted / max) * H;
          return (
            <g key={`${m.year}-${m.month}`}>
              <rect x={x} y={base - hp} width={w} height={hp} fill="var(--color-success)" />
              <rect x={x} y={base - hp - hh} width={w} height={hh} fill="var(--color-seal)" />
              <rect x={x} y={base - hp - hh - hd} width={w} height={hd} fill="var(--color-rubric)" />
              <text x={x + w / 2} y={base + 18} textAnchor="middle" fontSize="11" fill="var(--color-ink-soft)" fontFamily="var(--font-mono)">{monthName(m)}</text>
            </g>
          );
        })}
      </svg>
    </Card>
  );
}

export function LawyerStatement({ f }: { f: LawyerFinance }) {
  const { t } = useTranslation();
  return (
    <Card className="mb-4">
      <h2 className="font-display text-base font-bold">{t("lawyerFinance.statement")}</h2>
      <p className="mb-3 text-xs text-ink-faint">{t("lawyerFinance.statementHint")}</p>
      <div className="overflow-x-auto">
        <table className="w-full min-w-[720px] text-sm">
          <thead><tr className="border-b border-border text-xs text-ink-faint">
            <th className="py-2 text-start font-medium">{t("lawyerFinance.movement")}</th>
            <th className="py-2 text-center font-medium">{t("lawyerFinance.reference")}</th>
            <th className="py-2 text-center font-medium">{t("lawyerFinance.forHim")}</th>
            <th className="py-2 text-center font-medium">{t("lawyerFinance.againstHim")}</th>
            <th className="py-2 text-center font-medium">{t("lawyerFinance.balance")}</th>
            <th className="py-2 text-center font-medium">{t("lawyerFinance.date")}</th>
          </tr></thead>
          <tbody>
            {f.statement.map((l, i) => (
              <tr key={i} className="border-b border-rule">
                <td className="py-2">
                  {t(`lawyerFinance.kinds.${l.kind}`, l.kind)}
                  {l.kind === "PaidOut" && l.note && <span className="block text-xs text-ink-faint">{t("lawyerFinance.afterDebt", { amount: l.note })}</span>}
                  {l.kind === "RefundBeforePayout" && l.note && <span className="block text-xs text-ink-faint">{t(`refund.reasons.${l.note}`, l.note)}</span>}
                </td>
                <td className="py-2 text-center">
                  {l.paymentId ? <Link to={`/payments/${l.paymentId}`} className="text-seal-strong hover:underline"><Ltr className="font-mono">{l.reference}</Ltr></Link>
                    : l.reference ? <Ltr className="font-mono text-ink-soft">{l.reference}</Ltr> : "—"}
                </td>
                <td className="py-2 text-center">{l.amount > 0 && <Money value={l.amount} className="text-success" />}</td>
                <td className="py-2 text-center">{l.amount < 0 && <Money value={-l.amount} className="text-rubric" />}</td>
                <td className="py-2 text-center"><Money value={l.runningBalance} className={"font-semibold " + (l.runningBalance < 0 ? "text-rubric" : "")} /></td>
                <td className="py-2 text-center"><Ltr className="font-mono text-xs text-ink-faint">{formatDateTime(l.occurredAtUtc)}</Ltr></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {f.statement.length === 0 && <p className="py-6 text-center text-sm text-ink-faint">{t("lawyerFinance.noMovements")}</p>}
    </Card>
  );
}
