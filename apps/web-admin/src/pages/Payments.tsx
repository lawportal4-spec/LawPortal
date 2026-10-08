import { useEffect, useState } from "react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { Card, Input, Ltr, StatusTag } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDateTime } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { PageHeader } from "../components/PageHeader";
import { FilterBar, StatusTabs, filterSelectClass } from "../components/FilterBar";
import { Field } from "../components/DiscountCodeFields";
import { dayEnd, dayStart } from "../lib/api";
import { getPayments } from "../lib/financeApi";

const STATUSES = ["", "Initiated", "Paid", "Failed", "Refunded", "PartiallyRefunded"] as const;

export default function Payments() {
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const [status, setStatus] = useState("");
  // A link from a payment page (client or request) lands here already filtered.
  const [params] = useSearchParams();
  const [searchInput, setSearchInput] = useState(params.get("search") ?? "");
  const [search, setSearch] = useState(params.get("search") ?? "");
  const [purpose, setPurpose] = useState("");
  const [method, setMethod] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [minTotal, setMinTotal] = useState("");
  const [maxTotal, setMaxTotal] = useState("");
  const [discount, setDiscount] = useState<"" | "yes" | "no">("");
  const [payout, setPayout] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 15;

  // Search as the admin types, without a request per keystroke.
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const filters = {
    status: status || undefined,
    search: search || undefined,
    purpose: purpose || undefined,
    method: method || undefined,
    from: dayStart(from),
    to: dayEnd(to),
    minTotal: minTotal ? Number(minTotal) : undefined,
    maxTotal: maxTotal ? Number(maxTotal) : undefined,
    hasDiscount: discount ? discount === "yes" : undefined,
    payout: payout || undefined,
    page,
    pageSize,
  };
  const query = useQuery({
    queryKey: ["adminPayments", filters],
    queryFn: () => getPayments(filters),
    placeholderData: keepPreviousData,
  });
  const advancedActive = !!(purpose || method || from || to || minTotal || maxTotal || discount || payout);

  /** Every filter change starts again from page 1. */
  const set = <T,>(setter: (v: T) => void) => (v: T) => {
    setter(v);
    setPage(1);
  };

  function clearFilters() {
    setSearchInput("");
    setStatus("");
    setPurpose("");
    setMethod("");
    setFrom("");
    setTo("");
    setMinTotal("");
    setMaxTotal("");
    setDiscount("");
    setPayout("");
    setPage(1);
  }

  return (
    <AppShell>
      <PageHeader page="payments" />

      <StatusTabs values={STATUSES} value={status} onChange={set(setStatus)} label={(s) => t(`paymentStatus.${s || "all"}`)} />

      <FilterBar search={searchInput} onSearch={setSearchInput} placeholder={t("paymentsAdmin.search")}
        advancedActive={advancedActive} canClear={!!(advancedActive || status || search)} onClear={clearFilters}>
        <Field label={t("paymentsAdmin.purpose")}>
          <select id="pay-purpose" className={filterSelectClass} value={purpose} onChange={(e) => set(setPurpose)(e.target.value)}>
            <option value="">{t("paymentsAdmin.anyPurpose")}</option>
            <option value="RequestCheckout">{t("paymentsAdmin.purposes.RequestCheckout")}</option>
            <option value="WalletTopUp">{t("paymentsAdmin.purposes.WalletTopUp")}</option>
          </select>
        </Field>
        <Field label={t("paymentsAdmin.method")}>
          <select id="pay-method" className={filterSelectClass} value={method} onChange={(e) => set(setMethod)(e.target.value)}>
            <option value="">{t("paymentsAdmin.anyMethod")}</option>
            <option value="Card">{t("paymentsAdmin.methods.Card")}</option>
            <option value="Wallet">{t("paymentsAdmin.methods.Wallet")}</option>
          </select>
        </Field>
        <Field label={t("paymentsAdmin.discount")}>
          <select id="pay-discount" className={filterSelectClass} value={discount} onChange={(e) => set(setDiscount)(e.target.value as "" | "yes" | "no")}>
            <option value="">{t("paymentsAdmin.anyDiscount")}</option>
            <option value="yes">{t("paymentsAdmin.withDiscount")}</option>
            <option value="no">{t("paymentsAdmin.withoutDiscount")}</option>
          </select>
        </Field>
        <Field label={t("paymentsAdmin.payout")}>
          <select id="pay-payout" className={filterSelectClass} value={payout} onChange={(e) => set(setPayout)(e.target.value)}>
            <option value="">{t("paymentsAdmin.anyPayout")}</option>
            <option value="Held">{t("payout.statuses.Held")}</option>
            <option value="Released">{t("payout.statuses.Released")}</option>
            <option value="None">{t("paymentsAdmin.noPayout")}</option>
          </select>
        </Field>
        <Field label={t("paymentsAdmin.from")}>
          <Input id="pay-from" dir="ltr" type="date" value={from} onChange={(e) => set(setFrom)(e.target.value)} />
        </Field>
        <Field label={t("paymentsAdmin.to")}>
          <Input id="pay-to" dir="ltr" type="date" value={to} onChange={(e) => set(setTo)(e.target.value)} />
        </Field>
        <Field label={t("paymentsAdmin.minTotal")}>
          <Input id="pay-min" dir="ltr" type="number" min={0} inputMode="decimal" value={minTotal} onChange={(e) => set(setMinTotal)(e.target.value)} />
        </Field>
        <Field label={t("paymentsAdmin.maxTotal")}>
          <Input id="pay-max" dir="ltr" type="number" min={0} inputMode="decimal" value={maxTotal} onChange={(e) => set(setMaxTotal)(e.target.value)} />
        </Field>
      </FilterBar>

      {query.data?.items.length === 0 && <p className="text-sm text-ink-faint">{t("paymentsAdmin.empty")}</p>}

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}

      <div className="flex flex-col gap-2">
        {query.data?.items.map((p) => (
          <Link key={p.id} to={`/payments/${p.id}`}>
            <Card className="flex items-center justify-between gap-4 transition-colors hover:border-seal">
              <div>
                <p className="font-mono text-sm font-medium text-ink">{p.number}</p>
                <p className="text-xs text-ink-faint">
                  {p.clientName?.startsWith("+") ? <Ltr className="font-mono">{p.clientName}</Ltr> : p.clientName ?? "—"} → {p.lawyerName ?? "—"} ·{" "}
                  <Ltr>{formatDateTime(p.createdAtUtc)}</Ltr>
                </p>
              </div>
              <div className="flex items-center gap-3">
                <span className="font-mono text-sm text-ink-soft">
                  <Ltr>{formatCurrency(p.total)}</Ltr>
                </span>
                <StatusTag status={p.status} label={t(`paymentStatus.${p.status}`, p.status)} />
              </div>
            </Card>
          </Link>
        ))}
      </div>

      {query.data && (
        <div className="mt-8 flex items-center justify-center gap-4">
          <button
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-2 text-sm disabled:opacity-40"
          >
            <ChevronLeft className="h-4 w-4 rtl:hidden" />
            <ChevronRight className="h-4 w-4 ltr:hidden" />
            {isAr ? "السابق" : "Previous"}
          </button>
          <span className="font-mono text-sm text-ink-soft">
            {page} / {query.data.totalPages || 1}
          </span>
          <button
            disabled={page >= query.data.totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-2 text-sm disabled:opacity-40"
          >
            {isAr ? "التالي" : "Next"}
            <ChevronRight className="h-4 w-4 rtl:hidden" />
            <ChevronLeft className="h-4 w-4 ltr:hidden" />
          </button>
        </div>
      )}
    </AppShell>
  );
}
