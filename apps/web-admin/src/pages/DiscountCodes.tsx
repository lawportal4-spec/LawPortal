import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { CircleCheck, ChevronDown, ChevronLeft, ChevronRight, Plus, Share2 } from "lucide-react";
import { Button, Card, Input, Ltr, StatusTag } from "@law-portal/ui";
import { formatCurrency, formatDateTime, useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { PageHeader } from "../components/PageHeader";
import { FilterBar, StatusTabs, filterSelectClass } from "../components/FilterBar";
import { dayEnd, dayStart } from "../lib/api";
import { Field } from "../components/DiscountCodeFields";
import { CopyCodeButton } from "../components/CopyCodeButton";
import { ScopeChips } from "../components/ScopeChips";
import { ShareDiscountDialog } from "../components/ShareDiscountDialog";
import {
  DISCOUNT_SCOPES,
  getDiscountCode,
  getDiscountCodes,
  statusOf,
  type DiscountCodeListStatus,
  type DiscountKind,
  type AdminDiscountCodeDto,
  type DiscountScope,
} from "../lib/discountCodesApi";

const STATUS_TABS: (DiscountCodeListStatus | "")[] = ["", "Active", "Scheduled", "Expired", "UsedUp", "Inactive"];
const PAGE_SIZE = 20;

/** The list only: search, filters and paging. Creating and editing each have their own page. */
export default function DiscountCodes() {
  const { t } = useTranslation();
  const [status, setStatus] = useState<DiscountCodeListStatus | "">("");
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [scope, setScope] = useState<DiscountScope | "">("");
  const [kind, setKind] = useState<DiscountKind | "">("");
  const [validFrom, setValidFrom] = useState("");
  const [validTo, setValidTo] = useState("");
  const [page, setPage] = useState(1);
  const [sharing, setSharing] = useState<AdminDiscountCodeDto | null>(null);
  // Cards start on one line; "+N" on the scope chips opens one up.
  const [expanded, setExpanded] = useState<Set<string>>(new Set());
  const [overflow, setOverflow] = useState<Record<string, number>>({});
  const reportOverflow = (id: string) => (hidden: number) =>
    setOverflow((prev) => (prev[id] === hidden ? prev : { ...prev, [id]: hidden }));
  const toggle = (id: string) => setExpanded((prev) => {
    const next = new Set(prev);
    if (!next.delete(id)) next.add(id);
    return next;
  });

  // Just created or edited (see DiscountCodeNew / DiscountCodeEdit): confirm it and offer to share it.
  const location = useLocation();
  const navigate = useNavigate();
  const saved = location.state as { savedId?: string; action?: "created" | "updated" } | null;
  const createdId = saved?.savedId;
  const created = useQuery({ queryKey: ["discountCode", createdId], queryFn: () => getDiscountCode(createdId!), enabled: !!createdId });
  const dismissCreated = () => navigate(location.pathname, { replace: true, state: null });

  // Search as the admin types, without a request per keystroke.
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const filters = {
    search: search || undefined,
    status: status || undefined,
    scope: scope || undefined,
    kind: kind || undefined,
    validFrom: dayStart(validFrom),
    validTo: dayEnd(validTo),
    page,
    pageSize: PAGE_SIZE,
  };
  const query = useQuery({ queryKey: ["discountCodes", filters], queryFn: () => getDiscountCodes(filters), placeholderData: (prev) => prev });
  const advancedActive = !!(scope || kind || validFrom || validTo);

  function clearFilters() {
    setSearchInput("");
    setStatus("");
    setScope("");
    setKind("");
    setValidFrom("");
    setValidTo("");
    setPage(1);
  }

  return (
    <AppShell>
      <PageHeader
        page="discountCodes"
        actions={
          <Link to="/discount-codes/new">
            <Button>
              <Plus className="h-4 w-4" />
              {t("discount.admin.new")}
            </Button>
          </Link>
        }
      />

      <StatusTabs values={STATUS_TABS} value={status} onChange={(s) => { setStatus(s); setPage(1); }} label={(s) => (s ? t(`discount.admin.statusFilter.${s}`) : t("discount.admin.all"))} />

      <FilterBar search={searchInput} onSearch={setSearchInput} placeholder={t("discount.admin.search")}
        advancedActive={advancedActive} canClear={!!(advancedActive || status || search)} onClear={clearFilters}>
        <Field label={t("discount.admin.scopes")}>
          <select
            value={scope}
            onChange={(e) => { setScope(e.target.value as DiscountScope | ""); setPage(1); }}
            className={filterSelectClass}
          >
            <option value="">{t("discount.admin.anyScope")}</option>
            {DISCOUNT_SCOPES.map((s) => (
              <option key={s} value={s}>{t(`discount.scopes.${s}`)}</option>
            ))}
          </select>
        </Field>
        <Field label={t("discount.admin.kind")}>
          <select
            value={kind}
            onChange={(e) => { setKind(e.target.value as DiscountKind | ""); setPage(1); }}
            className={filterSelectClass}
          >
            <option value="">{t("discount.admin.anyKind")}</option>
            <option value="Percentage">{t("discount.admin.percentage")}</option>
            <option value="Fixed">{t("discount.admin.fixed")}</option>
          </select>
        </Field>
        <Field label={t("discount.admin.validFrom")}>
          <Input dir="ltr" type="date" value={validFrom} onChange={(e) => { setValidFrom(e.target.value); setPage(1); }} />
        </Field>
        <Field label={t("discount.admin.validTo")}>
          <Input dir="ltr" type="date" value={validTo} onChange={(e) => { setValidTo(e.target.value); setPage(1); }} />
        </Field>
      </FilterBar>

      {created.data && (
        <div role="status" className="mb-4 flex flex-wrap items-center justify-between gap-3 rounded-md border border-seal bg-seal-tint px-4 py-3">
          <p className="flex items-center gap-2 text-sm font-medium text-seal-strong">
            <CircleCheck className="h-5 w-5" />
            {t(saved?.action === "updated" ? "discount.share.updated" : "discount.share.created")}
            <Ltr className="font-mono">{created.data.code}</Ltr>
            <CopyCodeButton code={created.data.code} />
          </p>
          <div className="flex items-center gap-2">
            <Button onClick={() => setSharing(created.data)}>
              <Share2 className="h-4 w-4" />
              {t("discount.share.shareNow")}
            </Button>
            <Button variant="ghost" onClick={dismissCreated}>{t("discount.share.close")}</Button>
          </div>
        </div>
      )}

      {query.data && <p className="mb-3 text-xs text-ink-faint">{t("discount.admin.results", { count: query.data.totalCount })}</p>}
      {query.isError && <p className="text-sm text-rubric">{t("discount.admin.loadFailed")}</p>}
      {query.data?.items.length === 0 && <p className="text-sm text-ink-faint">{t("discount.admin.empty")}</p>}

      <div className="flex flex-col gap-2">
        {query.data?.items.map((c) => {
          const s = statusOf(c);
          return (
            // The whole card opens the code (a stretched link underneath); copy and share sit above it.
            <Card
              key={c.id}
              className={
                "relative flex flex-wrap items-center justify-between gap-4 transition-colors hover:border-seal " +
                (expanded.has(c.id) ? "" : "sm:flex-nowrap ") +
                (expanded.has(c.id) || (overflow[c.id] ?? 0) > 0 ? "pe-16 " : "") +
                (c.id === createdId ? "border-seal ring-2 ring-seal/30" : "")
              }
            >
              <Link to={`/discount-codes/${c.id}`} className="absolute inset-0 z-0 rounded-[inherit]" aria-label={c.code} />
              {/* Pinned to the card's corner so it stays put when the row expands. */}
              {(expanded.has(c.id) || (overflow[c.id] ?? 0) > 0) && (
                <button
                  type="button"
                  onClick={() => toggle(c.id)}
                  aria-expanded={expanded.has(c.id)}
                  aria-label={expanded.has(c.id) ? t("discount.admin.collapseRow") : t("discount.admin.expandRow")}
                  title={expanded.has(c.id) ? t("discount.admin.collapseRow") : t("discount.admin.expandRow")}
                  className="absolute end-5 top-8 z-10 flex h-8 w-8 items-center justify-center rounded-md border border-border text-ink-soft hover:border-seal hover:text-seal"
                >
                  <ChevronDown className={"h-4 w-4 transition-transform " + (expanded.has(c.id) ? "rotate-180" : "")} />
                </button>
              )}
                <div className={expanded.has(c.id) ? "min-w-0" : "min-w-0 flex-1"}>
                  <p className="flex items-center gap-2">
                    <Ltr className="font-mono font-semibold text-ink">{c.code}</Ltr>
                    <CopyCodeButton code={c.code} />
                    <Ltr className="font-mono text-sm text-seal">{c.kind === "Percentage" ? `${c.value}%` : formatCurrency(c.value)}</Ltr>
                  </p>
                  <ScopeChips labels={c.scopes.map((x) => t(`discount.scopes.${x}`))} expanded={expanded.has(c.id)} onToggle={() => toggle(c.id)} onOverflow={reportOverflow(c.id)} />
                </div>
                <div className={"flex items-center gap-4 text-xs text-ink-faint " + (expanded.has(c.id) ? "flex-wrap" : "shrink-0 flex-wrap sm:flex-nowrap sm:whitespace-nowrap")}>
                  <span>
                    {t("discount.admin.used")}:{" "}
                    <Ltr className="font-mono text-ink">
                      {c.used} / {c.usageLimit ?? "∞"}
                    </Ltr>
                  </span>
                  {(c.startsAtUtc || c.endsAtUtc) && (
                    <span>
                      {t("discount.admin.dates")}:{" "}
                      <Ltr className="font-mono text-ink">
                        {c.startsAtUtc ? formatDateTime(c.startsAtUtc) : "…"} → {c.endsAtUtc ? formatDateTime(c.endsAtUtc) : "…"}
                      </Ltr>
                    </span>
                  )}
                  <StatusTag status={s.tag} label={t(`discount.admin.statusFilter.${s.status}`)} />
                  <button
                    type="button"
                    onClick={() => setSharing(c)}
                    className="relative z-10 inline-flex items-center gap-1.5 rounded-md border border-border px-3 py-1.5 text-xs font-medium text-ink-soft hover:border-seal hover:text-seal"
                  >
                    <Share2 className="h-3.5 w-3.5" />
                    {t("discount.share.share")}
                  </button>
                </div>
            </Card>
          );
        })}
      </div>

      {query.data && query.data.totalPages > 1 && (
        <div className="mt-8 flex items-center justify-center gap-4">
          <button
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-2 text-sm disabled:opacity-40"
          >
            <ChevronLeft className="h-4 w-4 rtl:hidden" />
            <ChevronRight className="h-4 w-4 ltr:hidden" />
            {t("discount.admin.previous")}
          </button>
          <Ltr className="font-mono text-sm text-ink-soft">
            {page} / {query.data.totalPages}
          </Ltr>
          <button
            disabled={page >= query.data.totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-2 text-sm disabled:opacity-40"
          >
            {t("discount.admin.next")}
            <ChevronRight className="h-4 w-4 rtl:hidden" />
            <ChevronLeft className="h-4 w-4 ltr:hidden" />
          </button>
        </div>
      )}
      {sharing && <ShareDiscountDialog code={sharing} onClose={() => setSharing(null)} />}
    </AppShell>
  );
}
