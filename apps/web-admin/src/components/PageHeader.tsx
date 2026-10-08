import type { ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { Ltr, SectionHeading } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { getPageStats, type PageStatsPage } from "../lib/directoryApi";
import { Money, Stat } from "./directory/bits";

const TONE = { gold: "text-seal-strong", success: "text-success", danger: "text-rubric" } as const;

/** Every admin list page opens the same way: title, one-line description, then four summary cards. */
export function PageHeader({ page, actions }: { page: PageStatsPage; actions?: ReactNode }) {
  const { t } = useTranslation();
  const stats = useQuery({ queryKey: ["pageStats", page], queryFn: () => getPageStats(page) });
  return (
    <>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <SectionHeading level={2}>{t(`pageStats.${page}.title`)}</SectionHeading>
          <p className="mb-5 mt-1 text-sm text-ink-faint">{t(`pageStats.${page}.hint`)}</p>
        </div>
        {actions}
      </div>
      {stats.data && (
        <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {stats.data.map((s) => {
            // A danger tone only means something when there is something to worry about.
            const tone = s.tone && (s.tone !== "danger" || s.value > 0) ? TONE[s.tone] : "";
            return (
              <Stat key={s.key} label={t(`pageStats.${page}.${s.key}`)}>
                {s.isMoney ? <Money value={s.value} className={tone} /> : <Ltr className={"font-mono " + tone}>{s.value}</Ltr>}
              </Stat>
            );
          })}
        </div>
      )}
    </>
  );
}
