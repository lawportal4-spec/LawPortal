import { useEffect, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Card, Input, Ltr, SectionHeading } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { Field } from "../components/DiscountCodeFields";
import { FilterBar, StatusTabs, filterSelectClass } from "../components/FilterBar";
import { Stat } from "../components/directory/bits";
import { RequestsTable } from "../components/directory/RequestsTable";
import { dayEnd, dayStart } from "../lib/api";
import { getRequests } from "../lib/directoryApi";

const TYPES = ["Instant", "Scheduled", "Written", "Bidding", "Catalog"] as const;
const STATUSES = ["Draft", "Submitted", "Awarded", "Paid", "InProgress", "Completed", "Cancelled", "Refunded"] as const;

/** «الطلبات»: every service in the system. */
export default function Requests() {
  const { t } = useTranslation();
  const [text, setText] = useState("");
  const [search, setSearch] = useState("");
  const [type, setType] = useState("");
  const [status, setStatus] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);
  useEffect(() => { const id = setTimeout(() => { setSearch(text.trim()); setPage(1); }, 300); return () => clearTimeout(id); }, [text]);
  const query = useQuery({
    queryKey: ["adminRequests", search, type, status, from, to, page],
    queryFn: () => getRequests({ search: search || undefined, type: type || undefined, status: status || undefined, from: dayStart(from), to: dayEnd(to), page, pageSize: 20 }),
    placeholderData: keepPreviousData,
  });
  const s = query.data?.stats;
  const p = query.data?.page;
  const reset = <T,>(fn: (v: T) => void) => (v: T) => { fn(v); setPage(1); };

  function clearFilters() {
    setText("");
    setType("");
    setStatus("");
    setFrom("");
    setTo("");
    setPage(1);
  }

  return (
    <AppShell>
      <SectionHeading level={2}>{t("directory.requestsTitle")}</SectionHeading>
      <p className="mb-5 mt-1 text-sm text-ink-faint">{t("directory.requestsHint")}</p>
      {s && (
        <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <Stat label={t("directory.allRequests")}><Ltr className="font-mono">{s.total}</Ltr></Stat>
          <Stat label={t("directory.inProgress")}><Ltr className="font-mono">{s.inProgress}</Ltr></Stat>
          <Stat label={t("directory.completedThisMonth")}><Ltr className="font-mono text-success">{s.completedThisMonth}</Ltr></Stat>
          <Stat label={t("directory.withReports")}><Ltr className={"font-mono " + (s.withReports > 0 ? "text-rubric" : "")}>{s.withReports}</Ltr></Stat>
        </div>
      )}
      <StatusTabs values={["", ...STATUSES] as const} value={status as "" | (typeof STATUSES)[number]}
        onChange={reset(setStatus)} label={(x) => (x ? t(`directory.requestStatuses.${x}`) : t("directory.allStatuses"))} />
      <FilterBar search={text} onSearch={setText} placeholder={t("directory.requestsSearch")}
        advancedActive={!!(type || from || to)} canClear={!!(text || status || type || from || to)} onClear={clearFilters}>
        <Field label={t("directory.type")}>
          <select id="requests-type" className={filterSelectClass} value={type} onChange={(e) => reset(setType)(e.target.value)}>
            <option value="">{t("directory.allTypes")}</option>
            {TYPES.map((x) => <option key={x} value={x}>{t(`directory.types.${x}`)}</option>)}
          </select>
        </Field>
        <Field label={t("directory.from")}>
          <Input id="requests-from" dir="ltr" type="date" value={from} onChange={(e) => reset(setFrom)(e.target.value)} />
        </Field>
        <Field label={t("directory.to")}>
          <Input id="requests-to" dir="ltr" type="date" value={to} onChange={(e) => reset(setTo)(e.target.value)} />
        </Field>
      </FilterBar>
      <Card>
        {p && <RequestsTable rows={p.items} />}
        {p && p.totalPages > 1 && (
          <div className="mt-3 flex items-center justify-between text-sm text-ink-faint">
            <span>{t("directory.count", { count: p.totalCount })}</span>
            <span className="flex items-center gap-2">
              <button type="button" disabled={page <= 1} onClick={() => setPage(page - 1)} className="rounded-md border border-border px-3 py-1 disabled:opacity-40">{t("directory.previous")}</button>
              <Ltr className="font-mono">{page} / {p.totalPages}</Ltr>
              <button type="button" disabled={page >= p.totalPages} onClick={() => setPage(page + 1)} className="rounded-md border border-border px-3 py-1 disabled:opacity-40">{t("directory.next")}</button>
            </span>
          </div>
        )}
      </Card>
    </AppShell>
  );
}
