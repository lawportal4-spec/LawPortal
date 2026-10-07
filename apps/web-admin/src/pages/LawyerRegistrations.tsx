import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { Card, Input, Ltr, StatusTag } from "@law-portal/ui";
import { useTranslation, formatDate } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { PageHeader } from "../components/PageHeader";
import { FilterBar, StatusTabs, filterSelectClass } from "../components/FilterBar";
import { Field } from "../components/DiscountCodeFields";
import { getRegions } from "../lib/accountApi";
import { dayEnd, dayStart } from "../lib/api";
import { getLawyerRegistrations, type LicenseReviewStatus } from "../lib/adminApi";

const STATUS_TABS: (LicenseReviewStatus | "")[] = ["", "PendingReview", "ChangesRequested", "Approved", "Rejected"];
const PAGE_SIZE = 20;

/** Every lawyer registration, filterable by review status — opens into the review screen. */
export default function LawyerRegistrations() {
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const [params] = useSearchParams();
  const [status, setStatus] = useState<LicenseReviewStatus | "">(() => {
    const s = params.get("status");
    return s !== null && STATUS_TABS.includes(s as LicenseReviewStatus) ? (s as LicenseReviewStatus | "") : "PendingReview";
  });
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [regionId, setRegionId] = useState(0);
  const [cityId, setCityId] = useState(0);
  const [licenseType, setLicenseType] = useState("");
  const [accountStage, setAccountStage] = useState("");
  const [licenseExpiry, setLicenseExpiry] = useState("");
  const [submittedFrom, setSubmittedFrom] = useState("");
  const [submittedTo, setSubmittedTo] = useState("");
  const regions = useQuery({ queryKey: ["regions"], queryFn: getRegions, staleTime: Infinity });
  const cities = regions.data?.find((r) => r.id === regionId)?.cities ?? [];

  /** Every filter change starts again from page 1. */
  const set = <T,>(setter: (v: T) => void) => (v: T) => {
    setter(v);
    setPage(1);
  };

  // Wait for a pause in typing rather than querying per keystroke.
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const filters = {
    status: status || undefined,
    search: search || undefined,
    regionId: regionId || undefined,
    cityId: cityId || undefined,
    licenseType: licenseType || undefined,
    accountStage: accountStage || undefined,
    licenseExpiry: licenseExpiry || undefined,
    submittedFrom: dayStart(submittedFrom),
    submittedTo: dayEnd(submittedTo),
    page,
    pageSize: PAGE_SIZE,
  };
  const query = useQuery({
    queryKey: ["lawyerRegistrations", filters],
    queryFn: () => getLawyerRegistrations(filters),
    placeholderData: (prev) => prev,
  });
  const advancedActive = !!(regionId || licenseType || accountStage || licenseExpiry || submittedFrom || submittedTo);

  function clearFilters() {
    setSearchInput("");
    setStatus("");
    setRegionId(0);
    setCityId(0);
    setLicenseType("");
    setAccountStage("");
    setLicenseExpiry("");
    setSubmittedFrom("");
    setSubmittedTo("");
    setPage(1);
  }

  return (
    <AppShell>
      <PageHeader page="lawyers" />

      <StatusTabs values={STATUS_TABS} value={status} onChange={(s) => { setStatus(s); setPage(1); }} label={(s) => t(`lawyerReview.statuses.${s || "all"}`)} />

      <FilterBar search={searchInput} onSearch={setSearchInput} placeholder={t("lawyerReview.admin.search")}
        advancedActive={advancedActive} canClear={!!(advancedActive || status || search)} onClear={clearFilters}>
        <Field label={t("lawyerReview.filters.region")}>
          <select id="lr-region" className={filterSelectClass} value={regionId || ""} onChange={(e) => { set(setRegionId)(Number(e.target.value)); setCityId(0); }}>
            <option value="">{t("lawyerReview.filters.anyRegion")}</option>
            {regions.data?.map((r) => <option key={r.id} value={r.id}>{isAr ? r.nameAr : r.nameEn}</option>)}
          </select>
        </Field>
        <Field label={t("lawyerReview.filters.city")}>
          <select id="lr-city" className={filterSelectClass} value={cityId || ""} disabled={!regionId} onChange={(e) => set(setCityId)(Number(e.target.value))}>
            <option value="">{t("lawyerReview.filters.anyCity")}</option>
            {cities.map((c) => <option key={c.id} value={c.id}>{isAr ? c.nameAr : c.nameEn}</option>)}
          </select>
        </Field>
        <Field label={t("lawyerReview.filters.licenseType")}>
          <select id="lr-type" className={filterSelectClass} value={licenseType} onChange={(e) => set(setLicenseType)(e.target.value)}>
            <option value="">{t("lawyerReview.filters.anyLicenseType")}</option>
            <option value="Licensed">{t("lawyerAuth.register.licenseTypes.Licensed")}</option>
            <option value="Trainee">{t("lawyerAuth.register.licenseTypes.Trainee")}</option>
          </select>
        </Field>
        <Field label={t("lawyerReview.filters.licenseExpiry")}>
          <select id="lr-expiry" className={filterSelectClass} value={licenseExpiry} onChange={(e) => set(setLicenseExpiry)(e.target.value)}>
            <option value="">{t("lawyerReview.filters.anyExpiry")}</option>
            {(["Valid", "ExpiringSoon", "Expired"] as const).map((x) => <option key={x} value={x}>{t(`lawyerReview.filters.expiry.${x}`)}</option>)}
          </select>
        </Field>
        <Field label={t("lawyerReview.filters.accountStage")}>
          <select id="lr-stage" className={filterSelectClass} value={accountStage} onChange={(e) => set(setAccountStage)(e.target.value)}>
            <option value="">{t("lawyerReview.filters.anyStage")}</option>
            {(["VerifyEmail", "PayFee", "Active"] as const).map((x) => <option key={x} value={x}>{t(`lawyerReview.filters.stages.${x}`)}</option>)}
          </select>
        </Field>
        <Field label={t("lawyerReview.filters.submittedFrom")}>
          <Input id="lr-from" dir="ltr" type="date" value={submittedFrom} onChange={(e) => set(setSubmittedFrom)(e.target.value)} />
        </Field>
        <Field label={t("lawyerReview.filters.submittedTo")}>
          <Input id="lr-to" dir="ltr" type="date" value={submittedTo} onChange={(e) => set(setSubmittedTo)(e.target.value)} />
        </Field>
      </FilterBar>

      {query.data && <p className="mb-3 text-xs text-ink-faint">{t("lawyerReview.filters.results", { count: query.data.totalCount })}</p>}

      {query.isError && <p className="text-sm text-rubric">{t("lawyerReview.admin.loadFailed")}</p>}
      {query.data?.items.length === 0 && <p className="text-sm text-ink-faint">{t("lawyerReview.admin.empty")}</p>}

      <div className="flex flex-col gap-2">
        {query.data?.items.map((r) => (
          <Link key={r.lawyerProfileId} to={`/lawyers/${r.lawyerProfileId}`}>
            <Card className="flex flex-wrap items-center justify-between gap-4 transition-colors hover:border-seal">
              <div className="min-w-0">
                <p className="font-medium text-ink">{r.fullName}</p>
                <p className="flex flex-wrap gap-x-3 text-xs text-ink-faint">
                  {r.phoneE164 && <Ltr className="font-mono">{r.phoneE164}</Ltr>}
                  <Ltr className="font-mono">{r.email}</Ltr>
                </p>
              </div>
              <div className="flex flex-wrap items-center gap-4 text-sm">
                <span className="text-xs text-ink-faint">
                  {t("lawyerReview.admin.columns.licenseNumber")}: <Ltr className="font-mono text-ink">{r.licenseNumber}</Ltr>
                </span>
                <span className="text-xs text-ink-faint">
                  {r.resubmittedAtUtc ? t("lawyerReview.admin.resubmitted") : t("lawyerReview.admin.columns.submitted")}:{" "}
                  <Ltr className="font-mono text-ink">{formatDate(r.resubmittedAtUtc ?? r.submittedAtUtc)}</Ltr>
                </span>
                <StatusTag status={r.status} label={t(`lawyerReview.statuses.${r.status}`)} />
              </div>
            </Card>
          </Link>
        ))}
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
            {t("lawyerReview.admin.previous")}
          </button>
          <Ltr className="font-mono text-sm text-ink-soft">
            {page} / {query.data.totalPages}
          </Ltr>
          <button
            disabled={page >= query.data.totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-2 text-sm disabled:opacity-40"
          >
            {t("lawyerReview.admin.next")}
            <ChevronRight className="h-4 w-4 rtl:hidden" />
            <ChevronLeft className="h-4 w-4 ltr:hidden" />
          </button>
        </div>
      )}
    </AppShell>
  );
}
