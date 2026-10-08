import { useState } from "react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { Card, Input, Ltr } from "@law-portal/ui";
import { useTranslation, formatDateTime } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { PageHeader } from "../components/PageHeader";
import { Field } from "../components/DiscountCodeFields";
import { FilterBar } from "../components/FilterBar";
import { dayEnd, dayStart } from "../lib/api";
import { getAuditLogs } from "../lib/auditApi";

export default function AuditLog() {
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const [action, setAction] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const query = useQuery({
    queryKey: ["auditLogs", { action, from, to, page }],
    queryFn: () => getAuditLogs({ action: action || undefined, from: dayStart(from), to: dayEnd(to), page, pageSize }),
    placeholderData: keepPreviousData,
  });

  return (
    <AppShell>
      <PageHeader page="audit" />

      <FilterBar search={action} onSearch={(v) => { setAction(v); setPage(1); }} placeholder={t("auditLog.search")}
        advancedActive={!!(from || to)} canClear={!!(action || from || to)} onClear={() => { setAction(""); setFrom(""); setTo(""); setPage(1); }}>
        <Field label={t("directory.from")}>
          <Input id="audit-from" dir="ltr" type="date" value={from} onChange={(e) => { setFrom(e.target.value); setPage(1); }} />
        </Field>
        <Field label={t("directory.to")}>
          <Input id="audit-to" dir="ltr" type="date" value={to} onChange={(e) => { setTo(e.target.value); setPage(1); }} />
        </Field>
      </FilterBar>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}

      <div className="flex flex-col gap-2">
        {query.data?.items.map((log) => (
          <Card key={log.id} className="flex items-center justify-between gap-4">
            <div>
              <p className="font-mono text-sm font-medium text-ink">{log.action}</p>
              <p className="text-xs text-ink-faint">
                {log.entityType && `${log.entityType} · `}
                {log.entityId && <Ltr className="font-mono">{log.entityId}</Ltr>}
                {log.details && ` · ${log.details}`}
              </p>
            </div>
            <div className="text-end text-xs text-ink-faint">
              {log.actorRole && <p>{log.actorRole}</p>}
              <p className="font-mono">
                <Ltr>{formatDateTime(log.occurredAtUtc)}</Ltr>
              </p>
            </div>
          </Card>
        ))}
        {query.data?.items.length === 0 && <p className="text-sm text-ink-faint">{isAr ? "لا توجد أحداث مطابقة." : "No matching events."}</p>}
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
