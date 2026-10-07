import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { CircleCheck, ChevronLeft, ChevronRight, Plus, Search, Share2, SlidersHorizontal } from "lucide-react";
import { Button, Card, Chip, Input, Ltr, SectionHeading, StatusTag } from "@law-portal/ui";
import { formatCurrency, formatDateTime, useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { Field } from "../components/DiscountCodeFields";
import { CopyCodeButton } from "../components/CopyCodeButton";
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
/** "2026-10-07" (a date input) → start / end of that local day, as ISO. */
const dayStart = (d: string) => (d ? new Date(`${d}T00:00:00`).toISOString() : undefined);
const dayEnd = (d: string) => (d ? new Date(`${d}T23:59:59`).toISOString() : undefined);

/** The list only: search, filters and paging. Creating and editing each have their own page. */
export default function DiscountCodes() {
  const { t } = useTranslation();
  const [status, setStatus] = useState<DiscountCodeListStatus | "">("");
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [scope, setScope] = useState<DiscountScope | "">("");
  const [kind, setKind] = useState<DiscountKind | "">("");
  const [validFrom, setValidFrom] = useState("");
  const [validTo, setValidTo] = useState("");
  const [page, setPage] = useState(1);
  const [sharing, setSharing] = useState<AdminDiscountCodeDto | null>(null);

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
      <div className="mb-6 flex items-center justify-between gap-4">
        <SectionHeading level={2}>{t("discount.admin.title")}</SectionHeading>
        <Link to="/discount-codes/new">
          <Button>
            <Plus className="h-4 w-4" />
            {t("discount.admin.new")}
          </Button>
        </Link>
      </div>

      <div className="mb-4 flex flex-wrap gap-2" role="tablist">
        {STATUS_TABS.map((s) => (
          <button
            key={s || "all"}
            role="tab"
            aria-selected={status === s}
            onClick={() => {
              setStatus(s);
              setPage(1);
            }}
            className={
              status === s
                ? "rounded-full bg-seal px-4 py-1.5 text-sm font-medium text-seal-on"
                : "rounded-full border border-border px-4 py-1.5 text-sm text-ink-soft hover:border-seal hover:text-seal"
            }
          >
            {s ? t(`discount.admin.statusFilter.${s}`) : t("discount.admin.all")}
          </button>
        ))}
      </div>

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <Input
          icon={<Search className="h-4 w-4 text-ink-faint" />}
          className="w-full max-w-md"
          placeholder={t("discount.admin.search")}
          aria-label={t("discount.admin.search")}
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
        />
        <button
          type="button"
          aria-expanded={showAdvanced}
          onClick={() => setShowAdvanced((v) => !v)}
          className={
            "flex items-center gap-1.5 rounded-md border px-3 py-2 text-sm " +
            (advancedActive ? "border-seal text-seal" : "border-border text-ink-soft hover:border-seal hover:text-seal")
          }
        >
          <SlidersHorizontal className="h-4 w-4" />
          {showAdvanced ? t("discount.admin.hideAdvanced") : t("discount.admin.advanced")}
        </button>
        {(advancedActive || status || search) && (
          <button type="button" onClick={clearFilters} className="text-sm text-ink-faint hover:text-rubric">
            {t("discount.admin.clear")}
          </button>
        )}
      </div>

      {showAdvanced && (
        <Card className="mb-4 grid gap-4 sm:grid-cols-4">
          <Field label={t("discount.admin.scopes")}>
            <select
              value={scope}
              onChange={(e) => { setScope(e.target.value as DiscountScope | ""); setPage(1); }}
              className="rounded-md border border-border bg-surface-raised px-3 py-2.5 text-sm"
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
              className="rounded-md border border-border bg-surface-raised px-3 py-2.5 text-sm"
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
        </Card>
      )}

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
                (c.id === createdId ? "border-seal ring-2 ring-seal/30" : "")
              }
            >
              <Link to={`/discount-codes/${c.id}`} className="absolute inset-0 z-0 rounded-[inherit]" aria-label={c.code} />
                <div className="min-w-0">
                  <p className="flex items-center gap-2">
                    <Ltr className="font-mono font-semibold text-ink">{c.code}</Ltr>
                    <CopyCodeButton code={c.code} />
                    <Ltr className="font-mono text-sm text-seal">{c.kind === "Percentage" ? `${c.value}%` : formatCurrency(c.value)}</Ltr>
                  </p>
                  <div className="mt-1 flex flex-wrap gap-1">
                    {c.scopes.map((scopeName) => (
                      <Chip key={scopeName}>{t(`discount.scopes.${scopeName}`)}</Chip>
                    ))}
                  </div>
                </div>
                <div className="flex flex-wrap items-center gap-4 text-xs text-ink-faint">
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
