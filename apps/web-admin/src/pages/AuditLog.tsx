import { useState } from "react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { Card, Ltr } from "@law-portal/ui";
import { useTranslation, formatDateTime } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getAuditLogs } from "../lib/auditApi";

export default function AuditLog() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const [action, setAction] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const query = useQuery({
    queryKey: ["auditLogs", { action, page }],
    queryFn: () => getAuditLogs({ action: action || undefined, page, pageSize }),
    placeholderData: keepPreviousData,
  });

  return (
    <AppShell>
      <h1 className="mb-1 font-display text-2xl font-bold">{isAr ? "سجل التدقيق" : "Audit Log"}</h1>
      <p className="mb-6 text-sm text-ink-faint">
        {isAr ? `${query.data?.totalCount ?? "…"} حدث مسجَّل` : `${query.data?.totalCount ?? "…"} recorded events`}
      </p>

      <div className="mb-6">
        <input
          value={action}
          onChange={(e) => {
            setAction(e.target.value);
            setPage(1);
          }}
          placeholder={isAr ? "تصفية حسب اسم الإجراء (مثل LawyerVerified)" : "Filter by action name (e.g. LawyerVerified)"}
          className="w-full max-w-md rounded-md border border-border bg-surface-raised px-3 py-2.5 text-sm font-mono"
        />
      </div>

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
