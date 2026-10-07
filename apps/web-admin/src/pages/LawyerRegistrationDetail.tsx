import { useState, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, ArrowRight, CheckCircle2, ExternalLink, FileText, RotateCcw, TriangleAlert, XCircle } from "lucide-react";
import { Button, Card, Ltr, SectionHeading, StatusTag } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDate, formatDateTime, isoToHijriDate } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { ActivityList } from "../components/directory/ActivityList";
import { AdminNotes } from "../components/directory/AdminNotes";
import { Tabs } from "../components/directory/bits";
import { LawyerEarningsChart, LawyerFinanceBalance, LawyerStatement } from "../components/directory/LawyerFinancePanel";
import { RequestsTable } from "../components/directory/RequestsTable";
import { SuspendAccount } from "../components/directory/SuspendAccount";
import { ACCOUNT_STATUS_PILL, getLawyerFinance, getRequests } from "../lib/directoryApi";
import {
  CORRECTION_ISSUES,
  getLawyerRegistration,
  rejectLawyer,
  requestLawyerChanges,
  verifyLawyer,
  type LawyerRegistrationDetailDto,
  type LicenseCorrectionIssue,
} from "../lib/adminApi";

type Mode = "idle" | "changes" | "reject";

type LawyerTab = "finance" | "statement" | "requests" | "license" | "activity" | "notes";

/** One registration in full — personal data, location, licence with the uploaded file — and the
 * three decisions: approve, return for changes (checklist + note), or reject (reason). */
export default function LawyerRegistrationDetail() {
  const { id } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const query = useQuery({ queryKey: ["lawyerRegistration", id], queryFn: () => getLawyerRegistration(id!), enabled: !!id });
  const r = query.data;
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<LawyerTab>("finance");
  const finance = useQuery({ queryKey: ["lawyerFinance", id], queryFn: () => getLawyerFinance(id!), enabled: !!id });
  const requests = useQuery({ queryKey: ["lawyerRequests", id], queryFn: () => getRequests({ lawyerProfileId: id!, page: 1, pageSize: 50 }), enabled: !!id && tab === "requests" });

  return (
    <AppShell>
      <Link to="/lawyers" className="mb-6 inline-flex items-center gap-1.5 text-sm text-ink-faint hover:text-ink">
        {isAr ? <ArrowRight className="h-4 w-4" /> : <ArrowLeft className="h-4 w-4" />}
        {t("lawyerReview.admin.back")}
      </Link>

      {query.isError && <p className="text-sm text-rubric">{t("lawyerReview.admin.loadFailed")}</p>}

      {r && (
        <>
          <div className="mb-6 flex flex-wrap items-center gap-3">
            <SectionHeading level={2}>{r.fullName}</SectionHeading>
            <span className={"rounded-full px-2.5 py-0.5 text-xs " + ACCOUNT_STATUS_PILL[r.accountStatus]}>{t(`directory.accountStatuses.${r.accountStatus}`)}</span>
            <StatusTag status={r.status} label={t(`lawyerReview.statuses.${r.status}`)} />
            {r.onboardingStep && <StatusTag status="Pending" label={t(`lawyerOnboarding.admin.onboarding.${r.onboardingStep}`)} />}
            <span className="ms-auto">
              <SuspendAccount userId={r.userId} status={r.accountStatus} onDone={() => void queryClient.invalidateQueries({ queryKey: ["lawyerRegistration", id] })} />
            </span>
          </div>

          {r.formerAccount && r.formerAccount.debtBalance > 0 && (
            <div role="alert" className="mb-4 flex flex-wrap items-center gap-3 rounded-lg border border-rubric/50 bg-rubric-tint px-4 py-3 text-sm text-ink-soft">
              <TriangleAlert className="h-5 w-5 shrink-0 text-rubric" />
              <span className="min-w-0 flex-1">
                <b className="text-ink">{t("lawyerReview.admin.formerMatchTitle", { amount: formatCurrency(r.formerAccount.debtBalance) })}</b>
                <span className="block">{t("lawyerReview.admin.formerMatchBody", { name: r.formerAccount.fullName })}</span>
              </span>
              <Link to={`/lawyer-debts/${r.formerAccount.lawyerProfileId}`} className="text-seal-strong hover:underline">{t("lawyerReview.admin.formerMatchOpen")}</Link>
            </div>
          )}

          <Tabs<LawyerTab> value={tab} onChange={setTab} tabs={[
            { id: "finance", label: t("lawyerFinance.tabFinance") }, { id: "requests", label: t("lawyerFinance.tabRequests") },
            { id: "license", label: t("lawyerFinance.tabLicense") }, { id: "activity", label: t("directory.tabActivity") }, { id: "notes", label: t("directory.notes") },
          ]} />
          {tab === "finance" && finance.data && (
            <>
              <LawyerFinanceBalance f={finance.data} onWhy={() => setTab("statement")} />
              <LawyerEarningsChart f={finance.data} />
              <LawyerStatement f={finance.data} />
            </>
          )}
          {tab === "statement" && finance.data && <LawyerStatement f={finance.data} />}
          {tab === "requests" && <Card>{requests.data && <RequestsTable rows={requests.data.page.items} hide={["lawyer"]} />}</Card>}
          {tab === "activity" && <Card>{finance.data && <ActivityList items={finance.data.activity} />}</Card>}
          {tab === "notes" && <AdminNotes entityType="Lawyer" entityId={r.lawyerProfileId} />}
          {tab === "license" && (
            <>
            <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
              <div className="flex flex-col gap-4 lg:col-span-2">
                <Card>
                  <SectionHeading level={3} className="mb-3">
                    {t("lawyerReview.admin.sections.personal")}
                  </SectionHeading>
                  <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                    <Item label={t("lawyerReview.admin.fields.fullName")}>{r.fullName}</Item>
                    <Item label={t("lawyerReview.admin.fields.nationalId")}>
                      {r.nationalIdMasked ? <Ltr className="font-mono">{r.nationalIdMasked}</Ltr> : "—"}
                    </Item>
                    <Item label={t("lawyerReview.admin.fields.phone")}>
                      {r.phoneE164 ? <Ltr className="font-mono">{r.phoneE164}</Ltr> : "—"}
                      {r.isPhoneVerified && (
                        <span className="ms-2 inline-flex items-center gap-1 text-xs text-seal">
                          <CheckCircle2 className="h-3.5 w-3.5" />
                          {t("lawyerReview.admin.fields.phoneVerified")}
                        </span>
                      )}
                    </Item>
                    <Item label={t("lawyerReview.admin.fields.email")}>
                      <Ltr className="font-mono">{r.email}</Ltr>
                    </Item>
                    <Item label={t("lawyerReview.admin.fields.submitted")}>
                      <Ltr className="font-mono">{formatDateTime(r.submittedAtUtc)}</Ltr>
                    </Item>
                    <Item label={t("lawyerReview.admin.fields.terms")}>
                      {r.termsAcceptedAtUtc ? <Ltr className="font-mono">{formatDateTime(r.termsAcceptedAtUtc)}</Ltr> : "—"}
                    </Item>
                  </dl>
                </Card>

                <Card>
                  <SectionHeading level={3} className="mb-3">
                    {t("lawyerReview.admin.sections.location")}
                  </SectionHeading>
                  <dl className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                    <Item label={t("lawyerReview.admin.fields.country")}>{t(`lawyerAuth.register.countries.${r.countryCode}`)}</Item>
                    <Item label={t("lawyerReview.admin.fields.region")}>{(isAr ? r.regionNameAr : r.regionNameEn) ?? "—"}</Item>
                    <Item label={t("lawyerReview.admin.fields.city")}>{(isAr ? r.cityNameAr : r.cityNameEn) ?? "—"}</Item>
                  </dl>
                </Card>

                <Card>
                  <SectionHeading level={3} className="mb-3">
                    {t("lawyerReview.admin.sections.license")}
                  </SectionHeading>
                  <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                    <Item label={t("lawyerReview.admin.fields.licenseType")}>{t(`lawyerAuth.register.licenseTypes.${r.licenseType}`)}</Item>
                    <Item label={t("lawyerReview.admin.fields.licenseNumber")}>
                      <Ltr className="font-mono">{r.licenseNumber}</Ltr>
                    </Item>
                    <Item label={t("lawyerReview.admin.fields.issueDate")}>
                      <HijriAndGregorian iso={r.issueDate} />
                    </Item>
                    <Item label={t("lawyerReview.admin.fields.expiryDate")}>
                      <HijriAndGregorian iso={r.expiryDate} />
                    </Item>
                  </dl>
                </Card>

                <DocumentCard registration={r} />
              </div>

              <div className="flex flex-col gap-4">
                <DecisionCard registration={r} />
                <ContactCard registration={r} />
                <HistoryCard registration={r} />
              </div>
            </div>
            </>
          )}
        </>
      )}
    </AppShell>
  );
}

function Item({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <dt className="text-xs text-ink-faint">{label}</dt>
      <dd className="mt-0.5 text-sm text-ink">{children}</dd>
    </div>
  );
}

/** Licences are dated in Hijri, so that's what the reviewer compares against the file. */
function HijriAndGregorian({ iso }: { iso: string }) {
  const { t } = useTranslation();
  return (
    <>
      <Ltr className="font-mono">{isoToHijriDate(iso)}</Ltr>
      <span className="text-xs text-ink-faint"> {t("lawyerReview.admin.hijriSuffix")} · </span>
      <Ltr className="font-mono text-xs text-ink-faint">{formatDate(iso)}</Ltr>
    </>
  );
}

function DocumentCard({ registration: r }: { registration: LawyerRegistrationDetailDto }) {
  const { t } = useTranslation();
  const isImage = r.documentContentType?.startsWith("image/");
  return (
    <Card>
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <SectionHeading level={3}>{t("lawyerReview.admin.sections.document")}</SectionHeading>
        {r.documentUrl && (
          <a href={r.documentUrl} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1.5 text-sm font-medium text-seal hover:underline">
            <ExternalLink className="h-4 w-4" />
            {t("lawyerReview.admin.openDocument")}
          </a>
        )}
      </div>
      {!r.documentUrl && <p className="text-sm text-ink-faint">{t("lawyerReview.admin.noDocument")}</p>}
      {r.documentUrl && (
        <>
          <p className="mb-3 flex items-center gap-1.5 text-xs text-ink-faint">
            <FileText className="h-3.5 w-3.5" />
            <bdi>{r.documentFileName}</bdi>
          </p>
          {isImage ? (
            <img src={r.documentUrl} alt={r.documentFileName ?? ""} className="max-h-[32rem] w-full rounded-md border border-border object-contain" />
          ) : (
            <iframe src={r.documentUrl} title={r.documentFileName ?? "licence"} className="h-[32rem] w-full rounded-md border border-border bg-surface" />
          )}
        </>
      )}
    </Card>
  );
}

function DecisionCard({ registration: r }: { registration: LawyerRegistrationDetailDto }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [mode, setMode] = useState<Mode>("idle");
  const [issues, setIssues] = useState<LicenseCorrectionIssue[]>([]);
  const [note, setNote] = useState("");
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);

  const decide = useMutation({
    mutationFn: async (action: "approve" | "changes" | "reject") => {
      if (action === "approve") return verifyLawyer(r.lawyerProfileId);
      if (action === "changes") return requestLawyerChanges(r.lawyerProfileId, issues, note.trim());
      return rejectLawyer(r.lawyerProfileId, reason.trim());
    },
    onSuccess: () => {
      setMode("idle");
      setError(null);
      void queryClient.invalidateQueries({ queryKey: ["lawyerRegistration", r.lawyerProfileId] });
      void queryClient.invalidateQueries({ queryKey: ["lawyerRegistrations"] });
    },
    onError: () => setError(t("lawyerReview.admin.decision.failed")),
  });

  const canDecide = r.status === "PendingReview";

  return (
    <Card elevated>
      <SectionHeading level={3} className="mb-3">
        {t("lawyerReview.admin.sections.decision")}
      </SectionHeading>

      {!canDecide && (
        <p className="text-sm text-ink-faint">
          {t("lawyerReview.admin.decision.closed", { status: t(`lawyerReview.statuses.${r.status}`) })}
        </p>
      )}
      {r.onboardingStep && (
        <p className="mt-2 text-sm font-medium text-warning">{t(`lawyerOnboarding.admin.onboarding.${r.onboardingStep}`)}</p>
      )}

      {canDecide && mode === "idle" && (
        <div className="flex flex-col gap-2">
          <Button className="justify-center" disabled={decide.isPending} onClick={() => decide.mutate("approve")}>
            <CheckCircle2 className="h-4 w-4" />
            {t("lawyerReview.admin.decision.approve")}
          </Button>
          <Button variant="secondary" className="justify-center" onClick={() => setMode("changes")}>
            <RotateCcw className="h-4 w-4" />
            {t("lawyerReview.admin.decision.requestChanges")}
          </Button>
          <Button variant="danger" className="justify-center" onClick={() => setMode("reject")}>
            <XCircle className="h-4 w-4" />
            {t("lawyerReview.admin.decision.reject")}
          </Button>
        </div>
      )}

      {canDecide && mode === "changes" && (
        <form
          className="flex flex-col gap-3"
          onSubmit={(e) => {
            e.preventDefault();
            if (issues.length === 0 && !note.trim()) return setError(t("lawyerReview.admin.decision.needIssue"));
            decide.mutate("changes");
          }}
        >
          <fieldset className="flex flex-col gap-2">
            <legend className="mb-1 text-sm font-medium text-ink">{t("lawyerReview.admin.decision.issuesTitle")}</legend>
            {CORRECTION_ISSUES.map((issue) => (
              <label key={issue} className="flex items-start gap-2 text-sm text-ink-soft">
                <input
                  type="checkbox"
                  className="mt-0.5 h-4 w-4 accent-seal"
                  checked={issues.includes(issue)}
                  onChange={(e) => setIssues((prev) => (e.target.checked ? [...prev, issue] : prev.filter((i) => i !== issue)))}
                />
                {t(`lawyerReview.issues.${issue}`)}
              </label>
            ))}
          </fieldset>
          <label className="flex flex-col gap-1 text-sm text-ink-soft">
            {t("lawyerReview.admin.decision.note")}
            <textarea
              rows={3}
              maxLength={1000}
              value={note}
              onChange={(e) => setNote(e.target.value)}
              className="rounded-md border border-border bg-surface-raised px-3 py-2 text-sm text-ink focus:border-seal focus:outline-none focus:ring-2 focus:ring-seal/30"
            />
          </label>
          {error && <p className="text-xs text-rubric">{error}</p>}
          <Button type="submit" className="justify-center" disabled={decide.isPending}>
            {t("lawyerReview.admin.decision.sendBack")}
          </Button>
          <Button type="button" variant="ghost" className="justify-center" onClick={() => setMode("idle")}>
            {t("lawyerReview.admin.decision.cancel")}
          </Button>
        </form>
      )}

      {canDecide && mode === "reject" && (
        <form
          className="flex flex-col gap-3"
          onSubmit={(e) => {
            e.preventDefault();
            if (!reason.trim()) return setError(t("lawyerReview.admin.decision.needReason"));
            decide.mutate("reject");
          }}
        >
          <label className="flex flex-col gap-1 text-sm text-ink-soft">
            {t("lawyerReview.admin.decision.rejectReason")}
            <textarea
              rows={3}
              maxLength={500}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              className="rounded-md border border-border bg-surface-raised px-3 py-2 text-sm text-ink focus:border-seal focus:outline-none focus:ring-2 focus:ring-seal/30"
            />
          </label>
          {error && <p className="text-xs text-rubric">{error}</p>}
          <Button type="submit" variant="danger" className="justify-center" disabled={decide.isPending}>
            {t("lawyerReview.admin.decision.confirmReject")}
          </Button>
          <Button type="button" variant="ghost" className="justify-center" onClick={() => setMode("idle")}>
            {t("lawyerReview.admin.decision.cancel")}
          </Button>
        </form>
      )}
    </Card>
  );
}

function HistoryCard({ registration: r }: { registration: LawyerRegistrationDetailDto }) {
  const { t } = useTranslation();
  const hasHistory = r.correctionRequestedAtUtc || r.resubmittedAtUtc || r.rejectionReason || r.decidedAtUtc;
  if (!hasHistory) return null;
  return (
    <Card>
      <SectionHeading level={3} className="mb-3">
        {t("lawyerReview.admin.sections.history")}
      </SectionHeading>
      <dl className="flex flex-col gap-3">
        {r.correctionRequestedAtUtc && (
          <Item label={t("lawyerReview.admin.history.correctionRequested")}>
            <Ltr className="font-mono">{formatDateTime(r.correctionRequestedAtUtc)}</Ltr>
          </Item>
        )}
        {r.correctionIssues.length > 0 && (
          <Item label={t("lawyerReview.admin.history.requested")}>
            <ul className="list-disc ps-5">
              {r.correctionIssues.map((i) => (
                <li key={i}>{t(`lawyerReview.issues.${i}`)}</li>
              ))}
            </ul>
          </Item>
        )}
        {r.correctionNote && <Item label={t("lawyerReview.admin.history.note")}>{r.correctionNote}</Item>}
        {r.resubmittedAtUtc && (
          <Item label={t("lawyerReview.admin.history.resubmitted")}>
            <Ltr className="font-mono">{formatDateTime(r.resubmittedAtUtc)}</Ltr>
          </Item>
        )}
        {r.rejectionReason && <Item label={t("lawyerReview.admin.history.rejectionReason")}>{r.rejectionReason}</Item>}
        {r.decidedAtUtc && r.status !== "PendingReview" && (
          <Item label={t("lawyerReview.admin.history.decided")}>
            <Ltr className="font-mono">{formatDateTime(r.decidedAtUtc)}</Ltr>
          </Item>
        )}
      </dl>
    </Card>
  );
}

/** The lawyer's photo and the extra numbers they added for the platform team. */
function ContactCard({ registration: r }: { registration: LawyerRegistrationDetailDto }) {
  const { t } = useTranslation();
  return (
    <Card>
      <SectionHeading level={3} className="mb-3">
        {t("lawyerAccount.admin.contacts")}
      </SectionHeading>
      {r.photoUrl && (
        <img src={r.photoUrl} alt={t("lawyerAccount.admin.photo")} className="mb-4 h-24 w-24 rounded-full border border-border object-cover" />
      )}
      {r.contactNumbers.length === 0 ? (
        <p className="text-sm text-ink-faint">{t("lawyerAccount.admin.noContacts")}</p>
      ) : (
        <ul className="flex flex-col gap-2 text-sm">
          {r.contactNumbers.map((c) => (
            <li key={c.phoneE164 + c.kind} className="flex flex-wrap items-center justify-between gap-2">
              <span className="text-ink-soft">
                {t(`lawyerAccount.kinds.${c.kind}`)}
                {c.contactName && <span className="text-ink-faint"> · {c.contactName}</span>}
              </span>
              <Ltr className="font-mono text-ink">{c.phoneE164}</Ltr>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
