import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { ChevronLeft, ChevronRight, Search } from "lucide-react";
import { Card, Input, Ltr, SectionHeading, StatusTag } from "@law-portal/ui";
import { useTranslation, formatDate } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getLawyerRegistrations, type LicenseReviewStatus } from "../lib/adminApi";

const STATUS_TABS: (LicenseReviewStatus | "")[] = ["PendingReview", "ChangesRequested", "Approved", "Rejected", ""];
const PAGE_SIZE = 20;

/** Every lawyer registration, filterable by review status — opens into the review screen. */
export default function LawyerRegistrations() {
  const { t } = useTranslation();
  const [status, setStatus] = useState<LicenseReviewStatus | "">("PendingReview");
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);

  // Wait for a pause in typing rather than querying per keystroke.
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const query = useQuery({
    queryKey: ["lawyerRegistrations", status, search, page],
    queryFn: () => getLawyerRegistrations({ status: status || undefined, search: search || undefined, page, pageSize: PAGE_SIZE }),
  });

  return (
    <AppShell>
      <SectionHeading level={2} className="mb-1">
        {t("lawyerReview.admin.title")}
      </SectionHeading>
      <p className="mb-6 text-sm text-ink-faint">{t("lawyerReview.admin.subtitle")}</p>

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
            {t(`lawyerReview.statuses.${s || "all"}`)}
          </button>
        ))}
      </div>

      <Input
        icon={<Search className="h-4 w-4 text-ink-faint" />}
        className="mb-6 max-w-md"
        placeholder={t("lawyerReview.admin.search")}
        aria-label={t("lawyerReview.admin.search")}
        value={searchInput}
        onChange={(e) => setSearchInput(e.target.value)}
      />

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
