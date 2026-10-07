import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, ArrowRight } from "lucide-react";
import { Card, Ltr, SectionHeading, StatusTag } from "@law-portal/ui";
import { formatDate, formatDateTime, useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { ActivityList } from "../components/directory/ActivityList";
import { AdminNotes } from "../components/directory/AdminNotes";
import { Money, Stat, Tabs } from "../components/directory/bits";
import { RequestsTable } from "../components/directory/RequestsTable";
import { SuspendAccount } from "../components/directory/SuspendAccount";
import { ACCOUNT_STATUS_PILL, getClient } from "../lib/directoryApi";

type Tab = "requests" | "payments" | "wallet" | "activity" | "notes";

/** One client: who they are, every service they used, their payments, wallet and activity. */
export default function Client() {
  const { id } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const query = useQuery({ queryKey: ["adminClient", id], queryFn: () => getClient(id!), enabled: !!id });
  const [tab, setTab] = useState<Tab>("requests");
  const d = query.data;
  const s = d?.summary;

  return (
    <AppShell>
      <Link to="/clients" className="mb-4 inline-flex items-center gap-1.5 text-sm text-ink-faint hover:text-ink">
        {isAr ? <ArrowRight className="h-4 w-4" /> : <ArrowLeft className="h-4 w-4" />}{t("directory.backToClients")}
      </Link>
      {d && s && (
        <>
          <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
            <div>
              <SectionHeading level={2}>{s.fullName ?? <Ltr className="font-mono">{s.phoneE164}</Ltr>}</SectionHeading>
              <p className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-ink-faint">
                {s.fullName && <Ltr className="font-mono">{s.phoneE164}</Ltr>}
                <span>{(isAr ? s.cityNameAr : s.cityNameEn) ?? t("directory.noCity")}</span>
                <span>{t("directory.joined")} <Ltr className="font-mono">{formatDate(s.joinedAtUtc)}</Ltr></span>
                <span className={"rounded-full px-2.5 py-0.5 text-xs " + ACCOUNT_STATUS_PILL[s.status]}>{t(`directory.accountStatuses.${s.status}`)}</span>
                <span className={"rounded-full px-2.5 py-0.5 text-xs " + (d.pledgeAccepted ? "bg-info-tint text-info" : "bg-surface-raised text-ink-faint")}>
                  {d.pledgeAccepted ? t("directory.pledgeAccepted") : t("directory.pledgeNotAccepted")}
                </span>
              </p>
            </div>
            <SuspendAccount userId={s.userId} status={s.status} onDone={() => void queryClient.invalidateQueries({ queryKey: ["adminClient", id] })} />
          </div>

          <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Stat label={t("directory.totalPaid")}><Money value={s.totalPaid} /></Stat>
            <Stat label={t("directory.refunded")}><Money value={-d.totalRefunded} className={d.totalRefunded > 0 ? "text-rubric" : ""} /></Stat>
            <Stat label={t("directory.wallet")}><Money value={s.walletBalance} className="text-seal-strong" /></Stat>
            <Stat label={t("directory.requests")}><Ltr className="font-mono">{s.requestsCount}</Ltr></Stat>
          </div>

          <Tabs<Tab> value={tab} onChange={setTab} tabs={[
            { id: "requests", label: t("directory.tabRequests") }, { id: "payments", label: t("directory.tabPayments") },
            { id: "wallet", label: t("directory.tabWallet") }, { id: "activity", label: t("directory.tabActivity") }, { id: "notes", label: t("directory.notes") },
          ]} />
          {tab === "requests" && <Card><RequestsTable rows={d.requests} hide={["client"]} /></Card>}
          {tab === "payments" && (
            <Card>
              {d.payments.length === 0 && <p className="py-6 text-center text-sm text-ink-faint">{t("directory.noPayments")}</p>}
              {d.payments.map((p) => (
                <Link key={p.id} to={`/payments/${p.id}`} className="flex flex-wrap items-center justify-between gap-2 border-b border-rule py-2.5 text-sm last:border-0 hover:bg-surface-raised/40">
                  <span><Ltr className="font-mono text-seal-strong">{p.number}</Ltr> · {t(`paymentsAdmin.purposes.${p.purpose}`, p.purpose)} · {t(`paymentsAdmin.methods.${p.method}`, p.method)}
                    <span className="block text-xs text-ink-faint"><Ltr className="font-mono">{formatDateTime(p.createdAtUtc)}</Ltr></span></span>
                  <span className="flex items-center gap-2">
                    <Money value={p.total} />
                    {p.refunded > 0 && <Money value={-p.refunded} className="text-xs text-rubric" />}
                    <StatusTag status={p.status} label={t(`paymentStatus.${p.status}`, p.status)} />
                  </span>
                </Link>
              ))}
            </Card>
          )}
          {tab === "wallet" && (
            <Card>
              {d.wallet.length === 0 && <p className="py-6 text-center text-sm text-ink-faint">{t("directory.noWallet")}</p>}
              {d.wallet.map((w, i) => (
                <div key={i} className="flex items-center justify-between gap-2 border-b border-rule py-2.5 text-sm last:border-0">
                  <span>{t(`directory.walletTypes.${w.type}`, w.type)}
                    {w.paymentNumber && w.paymentId && <> · <Link to={`/payments/${w.paymentId}`} className="text-seal-strong hover:underline"><Ltr className="font-mono">{w.paymentNumber}</Ltr></Link></>}
                    <span className="block text-xs text-ink-faint"><Ltr className="font-mono">{formatDateTime(w.createdAtUtc)}</Ltr></span></span>
                  <Money value={w.amount} className={w.amount < 0 ? "text-rubric" : "text-success"} />
                </div>
              ))}
              {d.wallet.length > 0 && (
                <div className="flex items-center justify-between pt-2.5 text-sm font-semibold"><span>{t("directory.currentBalance")}</span><Money value={s.walletBalance} className="text-seal-strong" /></div>
              )}
            </Card>
          )}
          {tab === "activity" && <Card><ActivityList items={d.activity} /></Card>}
          {tab === "notes" && <AdminNotes entityType="Client" entityId={s.clientProfileId} />}
        </>
      )}
    </AppShell>
  );
}
