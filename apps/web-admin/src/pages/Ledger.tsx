import { useEffect, useState, type FormEvent, type ReactNode } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import {
  ArrowUpLeft, BookOpen, Check, ChevronDown, ChevronLeft, ChevronRight, CircleCheck, Equal, Percent, Plus, TableProperties, TriangleAlert,
} from "lucide-react";
import { Button, Card, Input, Ltr, SectionHeading } from "@law-portal/ui";
import { formatCurrency, formatDate, formatDateTime, useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { Field } from "../components/DiscountCodeFields";
import { FilterBar } from "../components/FilterBar";
import { dayEnd, dayStart } from "../lib/api";
import { getCategories } from "../lib/catalogApi";
import {
  JOURNAL_KINDS, LEDGER_ACCOUNTS, createCommissionPolicy, getCommissionPolicies, getLedgerJournal, getLedgerSummary,
  type LedgerAccount, type LedgerSummaryDto,
} from "../lib/financeApi";

type Group = "cash" | "receivable" | "owed" | "income" | "expense";
const GROUP_OF: Record<LedgerAccount, Group> = {
  ClearingGateway: "cash",
  LawyerReceivable: "receivable",
  RefundLossExpense: "expense",
  EscrowPayable: "owed", VatPayable: "owed", WalletLiability: "owed",
  CommissionRevenue: "income", SubscriptionRevenue: "income", RegistrationFeeRevenue: "income",
  DiscountExpense: "expense",
};
const GROUP_TAG: Record<Group, string> = {
  cash: "bg-info-tint text-info",
  receivable: "bg-info-tint text-info",
  owed: "bg-warning-tint text-warning",
  income: "bg-seal-tint text-seal-strong",
  expense: "bg-rubric-tint text-rubric",
};
const PERIODS = ["month", "quarter", "year", "all"] as const;
type Period = (typeof PERIODS)[number];
const selectClass = "rounded-md border border-border bg-surface-raised px-3 py-2.5 text-sm";

/** Riyadh is UTC+3 with no DST; periods start at Riyadh midnight, like the dashboard's «هذا الشهر». */
const RIYADH_MS = 3 * 60 * 60 * 1000;
const riyadhStart = (year: number, month: number) => new Date(Date.UTC(year, month, 1) - RIYADH_MS).toISOString();

function periodStart(p: Period): string | undefined {
  const now = new Date();
  const riyadh = new Date(now.getTime() + RIYADH_MS);
  if (p === "month") return riyadhStart(riyadh.getUTCFullYear(), riyadh.getUTCMonth());
  if (p === "quarter") return new Date(now.getFullYear(), now.getMonth() - 3, now.getDate()).toISOString();
  if (p === "year") return riyadhStart(riyadh.getUTCFullYear(), 0);
  return undefined;
}

const round2 = (n: number) => Math.round(n * 100) / 100;
const Money = ({ value, className = "" }: { value: number; className?: string }) => (
  <Ltr className={"font-mono tabular-nums " + className}>{formatCurrency(value)}</Ltr>
);

/** The money that actually moved for one entry: its cash line (gateway, else wallet); a payout fully
 * used to settle a debt has no cash line, so fall back to the largest line. */
function mainAmount(lines: { account: LedgerAccount; amount: number }[]) {
  const line = lines.find((l) => l.account === "ClearingGateway") ?? lines.find((l) => l.account === "WalletLiability");
  return line ? line.amount : Math.max(0, ...lines.map((l) => l.amount));
}

/** Where the money is, by who it belongs to — derived from the account balances. */
function moneyGroups(summary: LedgerSummaryDto) {
  const net = (a: LedgerAccount) => {
    const row = summary.accounts.find((x) => x.account === a);
    return row ? row.totalDebits - row.totalCredits : 0;
  };
  const owedLines = (["EscrowPayable", "VatPayable", "WalletLiability"] as const).map((a) => ({ account: a, amount: -net(a) }));
  const incomeLines = [
    ...(["CommissionRevenue", "SubscriptionRevenue", "RegistrationFeeRevenue"] as const).map((a) => ({ account: a as LedgerAccount, amount: -net(a) })),
    { account: "DiscountExpense" as LedgerAccount, amount: -net("DiscountExpense") },
    { account: "RefundLossExpense" as LedgerAccount, amount: -net("RefundLossExpense") },
  ].filter((l) => l.account !== "RefundLossExpense" || l.amount !== 0);
  // What the platform has: cash at the gateway plus what lawyers owe it back.
  const cashLines = [
    { account: "ClearingGateway" as LedgerAccount, amount: net("ClearingGateway") },
    { account: "LawyerReceivable" as LedgerAccount, amount: net("LawyerReceivable") },
  ].filter((l) => l.account === "ClearingGateway" || l.amount !== 0);
  const cash = round2(cashLines.reduce((s, l) => s + l.amount, 0));
  const owed = round2(owedLines.reduce((s, l) => s + l.amount, 0));
  const income = round2(incomeLines.reduce((s, l) => s + l.amount, 0));
  return { cash, cashLines, owed, income, owedLines, incomeLines, reconciles: round2(cash - owed - income) === 0 };
}

export default function Ledger() {
  const { t } = useTranslation();
  const [params] = useSearchParams();
  const [period, setPeriod] = useState<Period>(() => {
    const p = params.get("period");
    return PERIODS.includes(p as Period) ? (p as Period) : "all";
  });
  const summary = useQuery({ queryKey: ["ledgerSummary", period], queryFn: () => getLedgerSummary(periodStart(period)), placeholderData: keepPreviousData });

  return (
    <AppShell>
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <div>
          <SectionHeading level={2}>{t("ledger.title")}</SectionHeading>
          <p className="mt-1 text-sm text-ink-faint">{t("ledger.subtitle")}</p>
        </div>
        <div className="flex flex-col items-end gap-1.5">
          <div className="flex flex-wrap gap-2" role="group" aria-label={t("ledger.title")}>
            {PERIODS.map((p) => (
              <button
                key={p}
                type="button"
                aria-pressed={period === p}
                onClick={() => setPeriod(p)}
                className={
                  period === p
                    ? "rounded-full bg-seal px-4 py-1.5 text-sm font-medium text-seal-on"
                    : "rounded-full border border-border px-4 py-1.5 text-sm text-ink-soft hover:border-seal hover:text-seal"
                }
              >
                {t(`ledger.periods.${p}`)}
              </button>
            ))}
          </div>
          <p className="text-xs text-ink-faint" aria-live="polite">
            {periodStart(period) ? (
              <>
                {t("ledger.showingFrom")} <Ltr className="font-mono">{formatDate(periodStart(period)!)}</Ltr> {t("ledger.untilToday")}
              </>
            ) : (
              t("ledger.showingAll")
            )}
          </p>
        </div>
      </div>

      {summary.data && <Overview summary={summary.data} />}
      <CommissionRates />
      <Journal periodFrom={periodStart(period)} />
    </AppShell>
  );
}

function Overview({ summary }: { summary: LedgerSummaryDto }) {
  const { t } = useTranslation();
  const g = moneyGroups(summary);

  return (
    <>
      <Card className={"mb-4 flex flex-wrap items-center gap-x-6 gap-y-2 " + (summary.isBalanced ? "border-success/50" : "border-rubric")}>
        {summary.isBalanced ? (
          <span className="flex items-center gap-2 font-display text-base font-bold text-success">
            <CircleCheck className="h-6 w-6" />
            {t("ledger.balanced")}
          </span>
        ) : (
          <span className="flex items-center gap-2 font-display text-base font-bold text-rubric">
            <TriangleAlert className="h-6 w-6" />
            {t("ledger.unbalanced")}
          </span>
        )}
        <span className="ms-auto flex flex-wrap items-center gap-2 text-sm text-ink-soft">
          {t("ledger.totalDebit")} <Money value={summary.grandTotalDebits} className="text-ink" />
          <span>=</span>
          {t("ledger.totalCredit")} <Money value={summary.grandTotalCredits} className="text-ink" />
        </span>
      </Card>

      <div className="mb-4 grid gap-4 lg:grid-cols-3">
        <GroupCard group="cash" title={t("ledger.cash")} hint={t("ledger.cashHint")} total={g.cash}>
          {g.cashLines.map((l) =>
            l.account === "ClearingGateway" ? (
              <GroupLine key={l.account} label={t("ledger.cashLine")} hint={t("ledger.cashLineHint")} amount={l.amount} />
            ) : (
              <GroupLine key={l.account} label={t(`ledger.accountNames.${l.account}`)} hint={t(`ledger.accountHints.${l.account}`)} amount={l.amount} />
            ),
          )}
        </GroupCard>
        <GroupCard group="owed" title={t("ledger.owed")} hint={t("ledger.owedHint")} total={g.owed}>
          {g.owedLines.map((l) => (
            <GroupLine key={l.account} label={t(`ledger.accountNames.${l.account}`)} hint={t(`ledger.accountHints.${l.account}`)} amount={l.amount} />
          ))}
        </GroupCard>
        <GroupCard group="income" title={t("ledger.income")} hint={t("ledger.incomeHint")} total={g.income}>
          {g.incomeLines.map((l) => (
            <GroupLine key={l.account} label={t(`ledger.accountNames.${l.account}`)} amount={l.amount} />
          ))}
        </GroupCard>
      </div>

      <Card className="mb-4">
        <SectionHeading level={3} className="flex items-center gap-2">
          <Equal className="h-5 w-5 text-seal" />
          {t("ledger.equation")}
        </SectionHeading>
        <p className="mb-3 text-xs text-ink-faint">{t("ledger.equationHint")}</p>
        <div className="flex flex-wrap items-center justify-center gap-x-5 gap-y-3 rounded-md border border-rule bg-paper p-4 text-sm text-ink-soft">
          <EqTerm value={g.cash} label={t("ledger.cash")} className="text-info" />
          <span className="font-display text-xl text-ink-faint">=</span>
          <EqTerm value={g.owed} label={t("ledger.owed")} className="text-warning" />
          <span className="font-display text-xl text-ink-faint">+</span>
          <EqTerm value={g.income} label={t("ledger.income")} className="text-seal-strong" />
          {g.reconciles ? <Check className="h-6 w-6 text-success" /> : <TriangleAlert className="h-6 w-6 text-rubric" />}
        </div>
      </Card>

      <Card className="mb-4">
        <details className="group">
          <summary className="flex cursor-pointer list-none items-center justify-between gap-3 [&::-webkit-details-marker]:hidden">
            <span>
              <SectionHeading level={3} className="flex items-center gap-2">
                <TableProperties className="h-5 w-5 text-seal" />
                {t("ledger.accounts")}
              </SectionHeading>
              <span className="text-xs text-ink-faint">{t("ledger.accountsHint")}</span>
            </span>
            <ChevronDown className="h-5 w-5 text-ink-faint transition-transform group-open:rotate-180" />
          </summary>
          <div className="mt-4 overflow-x-auto">
            <table className="w-full min-w-[640px] text-sm">
              <thead>
                <tr className="border-b border-border text-xs text-ink-faint">
                  <th className="pb-2 text-start font-medium">{t("ledger.account")}</th>
                  <th className="pb-2 text-center font-medium">{t("ledger.debit")}</th>
                  <th className="pb-2 text-center font-medium">{t("ledger.credit")}</th>
                  <th className="pb-2 text-center font-medium">{t("ledger.balance")}</th>
                </tr>
              </thead>
              <tbody>
                {LEDGER_ACCOUNTS.map((account) => {
                  const row = summary.accounts.find((a) => a.account === account);
                  if (!row) return null;
                  return (
                    <tr key={account} className="border-b border-rule align-top">
                      <td className="py-2.5">
                        {t(`ledger.accountNames.${account}`)}
                        <GroupTag group={GROUP_OF[account]} />
                        <span className="block text-xs text-ink-faint">{t(`ledger.accountHints.${account}`)}</span>
                      </td>
                      <td className="py-2.5 text-center"><Money value={row.totalDebits} /></td>
                      <td className="py-2.5 text-center"><Money value={row.totalCredits} /></td>
                      <td className="py-2.5 text-center">
                        <Money value={Math.abs(row.netBalance)} />{" "}
                        <span className="text-xs text-ink-faint">{row.netBalance >= 0 ? t("ledger.debit") : t("ledger.credit")}</span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
              <tfoot>
                <tr className="font-semibold">
                  <td className="pt-2.5">{t("ledger.total")}</td>
                  <td className="pt-2.5 text-center"><Money value={summary.grandTotalDebits} /></td>
                  <td className="pt-2.5 text-center"><Money value={summary.grandTotalCredits} /></td>
                  <td className="pt-2.5 text-center"><Money value={summary.grandTotalDebits - summary.grandTotalCredits} /></td>
                </tr>
              </tfoot>
            </table>
          </div>
        </details>
      </Card>
    </>
  );
}

function GroupCard({ group, title, hint, total, children }: { group: Group; title: string; hint: string; total: number; children: ReactNode }) {
  const color = group === "cash" ? "text-info" : group === "owed" ? "text-warning" : "text-seal-strong";
  const dot = group === "cash" ? "bg-info" : group === "owed" ? "bg-warning" : "bg-seal-strong";
  return (
    <Card className="flex flex-col">
      <span className="flex items-center gap-2 text-sm text-ink-faint">
        <span className={"h-2.5 w-2.5 rounded-full " + dot} />
        {title}
      </span>
      <Money value={total} className={"mt-1 text-end text-3xl font-semibold " + color} />
      <p className="min-h-10 text-xs text-ink-faint">{hint}</p>
      <div className="mt-3 flex flex-col divide-y divide-dashed divide-rule border-t border-rule pt-1">{children}</div>
    </Card>
  );
}

function GroupLine({ label, hint, amount }: { label: string; hint?: string; amount: number }) {
  return (
    <div className="flex items-center justify-between gap-3 py-2 text-sm">
      <span className="flex flex-col">
        {label}
        {hint && <span className="text-xs text-ink-faint">{hint}</span>}
      </span>
      <Money value={amount} className={"shrink-0 whitespace-nowrap " + (amount < 0 ? "text-rubric" : "")} />
    </div>
  );
}

function EqTerm({ value, label, className }: { value: number; label: string; className: string }) {
  return (
    <span className="flex flex-col items-center">
      <Money value={value} className={"text-lg font-medium " + className} />
      {label}
    </span>
  );
}

function GroupTag({ group }: { group: Group }) {
  const { t } = useTranslation();
  // inline-block keeps the tag out of the word before it; inline, Arabic shaping would join the
  // last letter of the account name (e.g. the ع of «الدفع») to the tag's first letter.
  return <span className={"ms-2 inline-block rounded-full px-2 py-0.5 align-middle text-[11px] " + GROUP_TAG[group]}>{t(`ledger.groups.${group}`)}</span>;
}

function CommissionRates() {
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const policies = useQuery({ queryKey: ["commissionPolicies"], queryFn: getCommissionPolicies });
  const categories = useQuery({ queryKey: ["adminCategories"], queryFn: getCategories, staleTime: Infinity });
  const [category, setCategory] = useState("");
  const [percentage, setPercentage] = useState("");
  const [startsOn, setStartsOn] = useState("");
  const [tried, setTried] = useState(false);
  const [added, setAdded] = useState<string | null>(null);

  const nameOf = (slug: string | null) => {
    if (!slug) return t("ledger.allCategories");
    const c = categories.data?.find((x) => x.slug === slug);
    return c ? (isAr ? c.nameAr : c.nameEn) : slug;
  };

  const pct = Number(percentage);
  const categoryError = tried && !category ? t("form.required") : null;
  const pctError = !tried ? null : !percentage.trim() ? t("form.required") : !(pct >= 0 && pct <= 100) ? t("ledger.pctInvalid") : null;

  const create = useMutation({
    mutationFn: () => createCommissionPolicy(category === "all" ? null : category, pct, dayStart(startsOn)),
    onSuccess: () => {
      setAdded(t("ledger.rateAdded", { pct, category: nameOf(category === "all" ? null : category) }));
      setCategory(""); setPercentage(""); setStartsOn(""); setTried(false);
      void queryClient.invalidateQueries({ queryKey: ["commissionPolicies"] });
    },
  });

  function submit(e: FormEvent) {
    e.preventDefault();
    setTried(true);
    setAdded(null);
    if (!category || !percentage.trim() || !(pct >= 0 && pct <= 100)) return;
    create.mutate();
  }

  const generalRate = policies.data?.find((p) => p.serviceCategorySlug === null && p.state === "Applied");
  const stateClass = { Applied: "bg-success-tint text-success", Scheduled: "bg-warning-tint text-warning", Stopped: "bg-surface-raised text-ink-faint" };

  return (
    <Card className="mb-4">
      <details className="group">
        <summary className="flex cursor-pointer list-none items-center justify-between gap-3 [&::-webkit-details-marker]:hidden">
          <span>
            <SectionHeading level={3} className="flex items-center gap-2">
              <Percent className="h-5 w-5 text-seal" />
              {t("ledger.commission")}
            </SectionHeading>
            <span className="text-xs text-ink-faint">{t("ledger.commissionHint")}</span>
          </span>
          <span className="flex shrink-0 items-center gap-2 text-sm text-ink-soft">
            {generalRate && (
              <>
                {t("ledger.allCategories")} <Ltr className="font-mono font-semibold text-seal-strong">{generalRate.percentage}%</Ltr>
              </>
            )}
            <ChevronDown className="h-5 w-5 text-ink-faint transition-transform group-open:rotate-180" />
          </span>
        </summary>
        <div className="mt-4">

          <div className="flex flex-col gap-2">
            {policies.data?.map((p) => (
              <div key={p.id} className={"flex items-center justify-between gap-3 rounded-md border border-rule bg-paper px-4 py-2.5 " + (p.state === "Stopped" ? "opacity-60" : "")}>
                <span>
                  <span className="font-medium">{nameOf(p.serviceCategorySlug)}</span>
                  <span className={"ms-2 inline-block rounded-full px-2 py-0.5 text-[11px] " + stateClass[p.state]}>{t(`ledger.states.${p.state}`)}</span>
                  <span className="block text-xs text-ink-faint">
                    {p.state === "Scheduled" ? t("ledger.startsOn") : t("ledger.since")} <Ltr className="font-mono">{formatDate(p.effectiveFromUtc)}</Ltr>
                  </span>
                </span>
                <Ltr className="font-mono text-xl font-semibold text-seal-strong">{p.percentage}%</Ltr>
              </div>
            ))}
          </div>

          <form className="mt-4 grid gap-3 border-t border-rule pt-4 sm:grid-cols-[2fr_1fr_1.3fr_auto] sm:items-start" onSubmit={submit} noValidate>
            <Field label={t("ledger.category")} error={categoryError ?? undefined}>
              <select id="rate-category" className={selectClass + (categoryError ? " border-rubric" : "")} value={category} onChange={(e) => setCategory(e.target.value)}>
                <option value="">{t("ledger.chooseCategory")}</option>
                <option value="all">{t("ledger.allCategories")}</option>
                {categories.data?.map((c) => (
                  <option key={c.slug} value={c.slug}>{isAr ? c.nameAr : c.nameEn}</option>
                ))}
              </select>
            </Field>
            <Field label={t("ledger.percentage")} error={pctError ?? undefined}>
              <Input id="rate-percentage" dir="ltr" inputMode="decimal" value={percentage} className={pctError ? "border-rubric!" : undefined} onChange={(e) => setPercentage(e.target.value)} />
            </Field>
            <Field label={t("ledger.startsFrom")} hint={t("ledger.startsHint")}>
              <Input id="rate-starts" dir="ltr" type="date" value={startsOn} onChange={(e) => setStartsOn(e.target.value)} />
            </Field>
            <Button type="submit" className="sm:mt-5" disabled={create.isPending}>
              <Plus className="h-4 w-4" />
              {t("ledger.addRate")}
            </Button>
          </form>
          {added && <p className="mt-2 text-sm text-success">{added}</p>}
          {create.isError && <p className="mt-2 text-sm text-rubric">{t("ledger.rateFailed")}</p>}
        </div>
      </details>
    </Card>
  );
}

/** Follows the page's period unless the admin picks their own dates. */
function Journal({ periodFrom }: { periodFrom?: string }) {
  const { t } = useTranslation();
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [kind, setKind] = useState("");
  const [account, setAccount] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);

  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const filters = {
    search: search || undefined, kind: kind || undefined, account: account || undefined,
    from: dayStart(from) ?? periodFrom, to: dayEnd(to), page, pageSize: 10,
  };
  const journal = useQuery({ queryKey: ["ledgerJournal", filters], queryFn: () => getLedgerJournal(filters), placeholderData: keepPreviousData });
  const set = <T,>(setter: (v: T) => void) => (v: T) => { setter(v); setPage(1); };
  useEffect(() => setPage(1), [periodFrom]);
  const describe = (d: string | null) => (d ? t(`ledger.lineDescriptions.${d}`, d) : "");

  return (
    <Card>
      <SectionHeading level={3} className="flex items-center gap-2">
        <BookOpen className="h-5 w-5 text-seal" />
        {t("ledger.journal")}
      </SectionHeading>
      <p className="mb-4 text-xs text-ink-faint">{t("ledger.journalHint")}</p>

      <FilterBar search={searchInput} onSearch={setSearchInput} placeholder={t("ledger.journalSearch")}
        advancedActive={!!(kind || account || from || to)} canClear={!!(searchInput || kind || account || from || to)}
        onClear={() => { setSearchInput(""); setKind(""); setAccount(""); setFrom(""); setTo(""); setPage(1); }}>
        <Field label={t("ledger.kind")}>
          <select id="journal-kind" className={selectClass} value={kind} onChange={(e) => set(setKind)(e.target.value)}>
            <option value="">{t("ledger.allKinds")}</option>
            {JOURNAL_KINDS.map((k) => <option key={k} value={k}>{t(`ledger.kinds.${k}`)}</option>)}
          </select>
        </Field>
        <Field label={t("ledger.account")}>
          <select id="journal-account" className={selectClass} value={account} onChange={(e) => set(setAccount)(e.target.value)}>
            <option value="">{t("ledger.allAccounts")}</option>
            {LEDGER_ACCOUNTS.map((a) => <option key={a} value={a}>{t(`ledger.accountNames.${a}`)}</option>)}
          </select>
        </Field>
        <Field label={t("ledger.from")}>
          <Input id="journal-from" dir="ltr" type="date" value={from} onChange={(e) => set(setFrom)(e.target.value)} />
        </Field>
        <Field label={t("ledger.to")}>
          <Input id="journal-to" dir="ltr" type="date" value={to} onChange={(e) => set(setTo)(e.target.value)} />
        </Field>
      </FilterBar>

      {journal.data?.items.length === 0 && <p className="py-8 text-center text-sm text-ink-faint">{t("ledger.noEntries")}</p>}

      <div className="flex flex-col gap-3">
        {journal.data?.items.map((entry) => {
          const dr = round2(entry.lines.filter((l) => l.isDebit).reduce((s, l) => s + l.amount, 0));
          const cr = round2(entry.lines.filter((l) => !l.isDebit).reduce((s, l) => s + l.amount, 0));
          return (
            <details key={`${entry.referenceType}-${entry.referenceId}`} className="group overflow-hidden rounded-lg border border-rule bg-paper">
              <summary className="flex cursor-pointer list-none flex-wrap items-center gap-x-4 gap-y-1 bg-surface-raised px-4 py-2.5 [&::-webkit-details-marker]:hidden">
                <span className="font-display font-bold">{t(`ledger.kinds.${entry.kind}`, entry.kind)}</span>
                {entry.number &&
                  (entry.paymentId ? (
                    <Link to={`/payments/${entry.paymentId}`} className="inline-flex items-center gap-1 text-sm text-seal-strong hover:underline">
                      <Ltr className="font-mono">{entry.number}</Ltr>
                      <ArrowUpLeft className="h-3.5 w-3.5 ltr:rotate-90" />
                    </Link>
                  ) : (
                    <Ltr className="font-mono text-sm text-ink-soft">{entry.number}</Ltr>
                  ))}
                <span className={"inline-flex items-center gap-1 text-xs " + (dr === cr ? "text-success" : "text-rubric")}>
                  {dr === cr ? <Check className="h-3.5 w-3.5" /> : <TriangleAlert className="h-3.5 w-3.5" />}
                  {dr === cr ? t("ledger.entryBalanced") : t("ledger.entryUnbalanced")}
                </span>
                <span className="flex items-baseline gap-1.5 text-xs text-ink-faint">
                  {t(`ledger.amountLabels.${entry.kind}`, t("ledger.amountLabels.default"))}
                  <Money value={mainAmount(entry.lines)} className="text-sm font-semibold text-ink" />
                </span>
                <Ltr className="ms-auto font-mono text-xs text-ink-faint">{formatDateTime(entry.createdAtUtc)}</Ltr>
                <ChevronDown className="h-4 w-4 text-ink-faint transition-transform group-open:rotate-180" />
              </summary>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[560px] text-sm">
                  <thead>
                    <tr className="text-xs text-ink-faint">
                      <th className="px-4 py-2 text-start font-medium">{t("ledger.account")}</th>
                      <th className="px-4 py-2 text-start font-medium">{t("ledger.statement")}</th>
                      <th className="px-4 py-2 text-center font-medium">{t("ledger.debit")}</th>
                      <th className="px-4 py-2 text-center font-medium">{t("ledger.credit")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {entry.lines.map((l, i) => (
                      <tr key={i} className="border-t border-rule">
                        <td className="px-4 py-2">
                          {t(`ledger.accountNames.${l.account}`, l.account)}
                          <GroupTag group={GROUP_OF[l.account]} />
                        </td>
                        <td className="px-4 py-2 text-ink-soft">{describe(l.description)}</td>
                        <td className="px-4 py-2 text-center">{l.isDebit && <Money value={l.amount} />}</td>
                        <td className="px-4 py-2 text-center text-seal-strong">{!l.isDebit && <Money value={l.amount} />}</td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr className="border-t border-ink-faint font-semibold">
                      <td className="px-4 py-2" colSpan={2}>{t("ledger.sum")}</td>
                      <td className="px-4 py-2 text-center"><Money value={dr} /></td>
                      <td className="px-4 py-2 text-center"><Money value={cr} /></td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            </details>
          );
        })}
      </div>

      {journal.data && (
        <div className="mt-4 flex flex-wrap items-center justify-between gap-3 text-sm text-ink-faint">
          <span>{t("ledger.entries", { count: journal.data.totalCount })}</span>
          <span className="flex items-center gap-3">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 disabled:opacity-40"
            >
              <ChevronRight className="h-4 w-4 ltr:hidden" />
              <ChevronLeft className="h-4 w-4 rtl:hidden" />
              {t("ledger.previous")}
            </button>
            <Ltr className="font-mono">{page} / {journal.data.totalPages || 1}</Ltr>
            <button
              type="button"
              disabled={page >= journal.data.totalPages}
              onClick={() => setPage((p) => p + 1)}
              className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 disabled:opacity-40"
            >
              {t("ledger.next")}
              <ChevronLeft className="h-4 w-4 ltr:hidden" />
              <ChevronRight className="h-4 w-4 rtl:hidden" />
            </button>
          </span>
        </div>
      )}
    </Card>
  );
}
