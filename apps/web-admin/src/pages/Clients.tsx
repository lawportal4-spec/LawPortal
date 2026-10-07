import { useEffect, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { Card, Ltr, SectionHeading } from "@law-portal/ui";
import { formatDate, useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { Field } from "../components/DiscountCodeFields";
import { FilterBar, StatusTabs, filterSelectClass } from "../components/FilterBar";
import { Money, Stat } from "../components/directory/bits";
import { getRegions } from "../lib/accountApi";
import { ACCOUNT_STATUS_PILL, getClients } from "../lib/directoryApi";

/** «العملاء»: every client with their requests, payments and wallet. */
export default function Clients() {
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const navigate = useNavigate();
  const [text, setText] = useState("");
  const [search, setSearch] = useState("");
  const [regionId, setRegionId] = useState(0);
  const [cityId, setCityId] = useState(0);
  const [status, setStatus] = useState("");
  const [wallet, setWallet] = useState(false);
  const [page, setPage] = useState(1);
  useEffect(() => { const id = setTimeout(() => { setSearch(text.trim()); setPage(1); }, 300); return () => clearTimeout(id); }, [text]);
  const regions = useQuery({ queryKey: ["regions"], queryFn: getRegions, staleTime: Infinity });
  const cities = regions.data?.find((r) => r.id === regionId)?.cities ?? [];
  const query = useQuery({
    queryKey: ["adminClients", search, regionId, cityId, status, wallet, page],
    queryFn: () => getClients({ search: search || undefined, regionId: regionId || undefined, cityId: cityId || undefined, status: status || undefined, hasWalletBalance: wallet || undefined, page, pageSize: 20 }),
    placeholderData: keepPreviousData,
  });
  const s = query.data?.stats;
  const p = query.data?.page;

  function clearFilters() {
    setText("");
    setStatus("");
    setRegionId(0);
    setCityId(0);
    setWallet(false);
    setPage(1);
  }

  return (
    <AppShell>
      <SectionHeading level={2}>{t("directory.clients")}</SectionHeading>
      <p className="mb-5 mt-1 text-sm text-ink-faint">{t("directory.clientsHint")}</p>
      {s && (
        <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <Stat label={t("directory.totalClients")}><Ltr className="font-mono">{s.total}</Ltr></Stat>
          <Stat label={t("directory.activeThisMonth")}><Ltr className="font-mono">{s.activeThisMonth}</Ltr></Stat>
          <Stat label={t("directory.walletTotal")}><Money value={s.walletTotal} className="text-seal-strong" /></Stat>
          <Stat label={t("directory.totalPaid")}><Money value={s.totalPaid} /></Stat>
        </div>
      )}
      <StatusTabs values={["", "Active", "Suspended", "Deleted"] as const} value={status as "" | "Active" | "Suspended" | "Deleted"}
        onChange={(x) => { setStatus(x); setPage(1); }} label={(x) => (x ? t(`directory.accountStatuses.${x}`) : t("directory.allStatuses"))} />
      <FilterBar search={text} onSearch={setText} placeholder={t("directory.clientsSearch")}
        advancedActive={!!(regionId || wallet)} canClear={!!(text || status || regionId || wallet)} onClear={clearFilters}>
        <Field label={t("directory.region")}>
          <select id="clients-region" className={filterSelectClass} value={regionId || ""} onChange={(e) => { setRegionId(Number(e.target.value)); setCityId(0); setPage(1); }}>
            <option value="">{t("directory.allRegions")}</option>
            {regions.data?.map((r) => <option key={r.id} value={r.id}>{isAr ? r.nameAr : r.nameEn}</option>)}
          </select>
        </Field>
        <Field label={t("directory.city")}>
          <select id="clients-city" className={filterSelectClass} value={cityId || ""} disabled={!regionId} onChange={(e) => { setCityId(Number(e.target.value)); setPage(1); }}>
            <option value="">{t("directory.allCities")}</option>
            {cities.map((c) => <option key={c.id} value={c.id}>{isAr ? c.nameAr : c.nameEn}</option>)}
          </select>
        </Field>
        <Field label={t("directory.wallet")}>
          <select id="clients-wallet" className={filterSelectClass} value={wallet ? "yes" : ""} onChange={(e) => { setWallet(e.target.value === "yes"); setPage(1); }}>
            <option value="">{t("directory.anyWallet")}</option>
            <option value="yes">{t("directory.hasWallet")}</option>
          </select>
        </Field>
      </FilterBar>
      <Card>
        {p?.items.length === 0 && <p className="py-8 text-center text-sm text-ink-faint">{t("directory.noResults")}</p>}
        {!!p?.items.length && (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] text-sm">
              <thead><tr className="border-b border-border text-xs text-ink-faint">
                <th className="py-2 text-start font-medium">{t("directory.client")}</th>
                <th className="py-2 text-center font-medium">{t("directory.city")}</th>
                <th className="py-2 text-center font-medium">{t("directory.requests")}</th>
                <th className="py-2 text-center font-medium">{t("directory.totalPaid")}</th>
                <th className="py-2 text-center font-medium">{t("directory.wallet")}</th>
                <th className="py-2 text-center font-medium">{t("directory.lastActivity")}</th>
                <th className="py-2 text-center font-medium">{t("directory.status")}</th>
              </tr></thead>
              <tbody>
                {p.items.map((c) => (
                  <tr key={c.clientProfileId} className="cursor-pointer border-b border-rule hover:bg-surface-raised/40" onClick={() => navigate(`/clients/${c.clientProfileId}`)}>
                    <td className="py-2.5">{c.fullName ?? "—"}<span className="block text-xs text-ink-faint"><Ltr className="font-mono">{c.phoneE164 ?? "—"}</Ltr></span></td>
                    <td className="py-2.5 text-center">{(isAr ? c.cityNameAr : c.cityNameEn) ?? "—"}</td>
                    <td className="py-2.5 text-center"><Ltr className="font-mono">{c.requestsCount}</Ltr></td>
                    <td className="py-2.5 text-center"><Money value={c.totalPaid} /></td>
                    <td className="py-2.5 text-center"><Money value={c.walletBalance} className={c.walletBalance > 0 ? "text-seal-strong" : ""} /></td>
                    <td className="py-2.5 text-center">{c.lastActivityUtc ? <Ltr className="font-mono">{formatDate(c.lastActivityUtc)}</Ltr> : "—"}</td>
                    <td className="py-2.5 text-center"><span className={"inline-block rounded-full px-2.5 py-0.5 text-xs " + ACCOUNT_STATUS_PILL[c.status]}>{t(`directory.accountStatuses.${c.status}`)}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
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
