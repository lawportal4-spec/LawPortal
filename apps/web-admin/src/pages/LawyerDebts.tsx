import { useState, type ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { Card, Ltr, SectionHeading } from "@law-portal/ui";
import { formatCurrency, formatDate, useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { COLLECTION_PILL, getLawyerDebts } from "../lib/lawyerDebtsApi";

const FILTERS = ["", "Offsetting", "LeftPlatform", "Settled"] as const;

/** «مبالغ مستحقة على المحامين»: lawyers who owe the platform from refunds after they were paid. */
export default function LawyerDebts() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [filter, setFilter] = useState<(typeof FILTERS)[number]>("");
  const query = useQuery({ queryKey: ["lawyerDebts", filter], queryFn: () => getLawyerDebts(filter || undefined) });
  const s = query.data?.stats;

  return (
    <AppShell>
      <SectionHeading level={2}>{t("debts.title")}</SectionHeading>
      <p className="mb-5 mt-1 text-sm text-ink-faint">{t("debts.subtitle")}</p>

      {s && (
        <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <Stat label={t("debts.totalOutstanding")}><Ltr className="font-mono text-seal-strong">{formatCurrency(s.totalOutstanding)}</Ltr></Stat>
          <Stat label={t("debts.lawyersOwing")}><Ltr className="font-mono">{s.lawyersOwing}</Ltr></Stat>
          <Stat label={t("debts.collectedThisMonth")}><Ltr className="font-mono text-success">{formatCurrency(s.collectedThisMonth)}</Ltr></Stat>
          <Stat label={t("debts.leftPlatform")}>
            <Ltr className="font-mono text-rubric">{s.leftPlatformCount} · {formatCurrency(s.leftPlatformAmount)}</Ltr>
          </Stat>
        </div>
      )}

      <Card>
        <div className="mb-3 flex flex-wrap gap-2" role="tablist">
          {FILTERS.map((f) => (
            <button
              key={f || "all"}
              role="tab"
              aria-selected={filter === f}
              onClick={() => setFilter(f)}
              className={
                filter === f
                  ? "rounded-full bg-seal px-4 py-1.5 text-sm font-medium text-seal-on"
                  : "rounded-full border border-border px-4 py-1.5 text-sm text-ink-soft hover:border-seal hover:text-seal"
              }
            >
              {t(`debts.filters.${f || "all"}`)}
            </button>
          ))}
        </div>

        {query.data?.items.length === 0 && <p className="py-8 text-center text-sm text-ink-faint">{t("debts.empty")}</p>}

        {!!query.data?.items.length && (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[680px] text-sm">
              <thead>
                <tr className="border-b border-border text-xs text-ink-faint">
                  <th className="py-2 text-start font-medium">{t("debts.lawyer")}</th>
                  <th className="py-2 text-center font-medium">{t("debts.balance")}</th>
                  <th className="py-2 text-center font-medium">{t("debts.oldest")}</th>
                  <th className="py-2 text-center font-medium">{t("debts.payments")}</th>
                  <th className="py-2 text-center font-medium">{t("debts.collection")}</th>
                </tr>
              </thead>
              <tbody>
                {query.data.items.map((d) => (
                  <tr
                    key={d.lawyerProfileId}
                    className="cursor-pointer border-b border-rule hover:bg-surface-raised/40"
                    onClick={() => navigate(`/lawyer-debts/${d.lawyerProfileId}`)}
                  >
                    <td className="py-3">
                      {d.fullName}
                      <span className="block text-xs text-ink-faint">
                        {d.accountDeleted && d.deletedAtUtc
                          ? t("debts.leftOn", { date: formatDate(d.deletedAtUtc) })
                          : t("debts.activeAccount")}
                      </span>
                    </td>
                    <td className="py-3 text-center">
                      <Ltr className={"font-mono font-semibold " + (d.accountDeleted ? "text-rubric" : "text-seal-strong")}>{formatCurrency(d.balance)}</Ltr>
                    </td>
                    <td className="py-3 text-center">
                      {d.oldestOpenDebtAtUtc ? <Ltr className="font-mono">{formatDate(d.oldestOpenDebtAtUtc)}</Ltr> : "—"}
                    </td>
                    <td className="py-3 text-center"><Ltr className="font-mono">{d.paymentsCount}</Ltr></td>
                    <td className="py-3 text-center">
                      <span className={"inline-block rounded-full px-2.5 py-0.5 text-xs " + COLLECTION_PILL[d.collection]}>
                        {t(`debts.collections.${d.collection}`)}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </AppShell>
  );
}

function Stat({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Card className="flex flex-col gap-1 py-3">
      <span className="text-xs text-ink-faint">{label}</span>
      <span className="text-xl font-semibold">{children}</span>
    </Card>
  );
}
