import { Ltr } from "@law-portal/ui";
import { formatDateTime, useTranslation } from "@law-portal/i18n";
import type { ActivityItem } from "../../lib/directoryApi";

/** Audit-log entries as a timeline: what happened, who did it, when. */
export function ActivityList({ items }: { items: ActivityItem[] }) {
  const { t } = useTranslation();
  if (items.length === 0) return <p className="py-6 text-center text-sm text-ink-faint">{t("directory.noActivity")}</p>;
  return (
    <ol className="flex flex-col">
      {items.map((a, i) => (
        <li key={i} className="flex items-start justify-between gap-3 border-b border-rule py-2.5 text-sm last:border-0">
          <span>
            {t(`directory.actions.${a.action}`, a.action)}
            {a.details && <span className="block text-xs text-ink-soft">{a.details.split(" · ").map((part, j) => <span key={j}>{j > 0 && " · "}<bdi>{t(`refund.reasons.${part}`, part)}</bdi></span>)}</span>}
            <span className="block text-xs text-info">{a.actorName ?? (a.actorRole ? t(`directory.roles.${a.actorRole}`, a.actorRole) : t("directory.system"))}</span>
          </span>
          <Ltr className="shrink-0 font-mono text-xs text-ink-faint">{formatDateTime(a.occurredAtUtc)}</Ltr>
        </li>
      ))}
    </ol>
  );
}
