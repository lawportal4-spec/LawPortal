import { useState, type FormEvent, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { AlertTriangle, Lock, XCircle } from "lucide-react";
import { Button, Card, Input, Ltr } from "@law-portal/ui";
import { hijriToIsoDate, isoToHijriDate, useTranslation } from "@law-portal/i18n";
import { useAuth } from "../lib/authContext";
import { resubmitLicense, type LawyerMeDto, type LicenseType } from "../lib/authApi";
import { AuthLayout, Field, Select } from "./AuthForm";
import { HijriDateInput } from "./HijriDateInput";
import { LicenseUpload } from "./LicenseUpload";
import { ACCEPTED_TYPES, MAX_FILE_BYTES } from "../lib/licenseFile";
import { Accepted, CheckEmail } from "./Onboarding";

const FILE_ISSUES = ["FileUnreadable", "WrongFile"];

/** What a lawyer whose account isn't open yet sees instead of the portal: the review status, or
 * after approval the remaining onboarding step (confirm the email, then pay the fee). */
export function RegistrationStatus({ me }: { me: LawyerMeDto }) {
  const navigate = useNavigate();
  return (
    <AuthLayout hideNav>
      {me.onboardingStep === "VerifyEmail" ? (
        <CheckEmail me={me} />
      ) : me.onboardingStep === "PayFee" ? (
        <Accepted name={me.fullName} onStart={() => navigate("/registration-fee")} />
      ) : me.reviewStatus === "ChangesRequested" ? (
        <ChangesRequested me={me} />
      ) : me.reviewStatus === "Rejected" ? (
        <Rejected me={me} />
      ) : (
        <UnderReview me={me} />
      )}
    </AuthLayout>
  );
}

function LogoutButton() {
  const { t } = useTranslation();
  const { logout } = useAuth();
  const navigate = useNavigate();
  return (
    <Button
      variant="secondary"
      onClick={() => {
        logout();
        navigate("/login", { replace: true });
      }}
    >
      {t("lawyerAuth.pending.logout")}
    </Button>
  );
}

function Centered({ children }: { children: ReactNode }) {
  return <div className="flex flex-col items-center gap-4 text-center">{children}</div>;
}

function UnderReview({ me }: { me: LawyerMeDto }) {
  const { t } = useTranslation();
  return (
    <Centered>
      <p className="text-lg text-ink">{t("lawyerAuth.pending.greeting", { name: me.fullName })}</p>
      <p className="font-display text-2xl text-seal">{t("lawyerAuth.pending.thanks")}</p>
      <p className="text-sm text-ink-faint">{t("lawyerAuth.pending.review")}</p>
      <div className="mt-4">
        <LogoutButton />
      </div>
    </Centered>
  );
}

function Rejected({ me }: { me: LawyerMeDto }) {
  const { t } = useTranslation();
  return (
    <Centered>
      <XCircle className="h-12 w-12 text-rubric" />
      <p className="text-lg text-ink">{t("lawyerAuth.pending.greeting", { name: me.fullName })}</p>
      <p className="font-display text-2xl text-ink">{t("lawyerReview.lawyer.rejectedTitle")}</p>
      {me.rejectionReason && (
        <Card className="w-full text-start">
          <p className="text-xs text-ink-faint">{t("lawyerReview.lawyer.rejectedReason")}</p>
          <p className="mt-1 text-sm text-ink">{me.rejectionReason}</p>
        </Card>
      )}
      <div className="mt-2">
        <LogoutButton />
      </div>
    </Centered>
  );
}

function ChangesRequested({ me }: { me: LawyerMeDto }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const fileRequired = me.correctionIssues.some((i) => FILE_ISSUES.includes(i));
  // Only what the admin flagged opens for editing; with no checklist item (a note alone) we
  // can't tell what to fix, so everything stays open. The API applies the same rule.
  const flagged = (issue: string) => me.correctionIssues.length === 0 || me.correctionIssues.includes(issue);
  const canNumber = flagged("LicenseNumberMismatch");
  const canDates = flagged("DatesMismatch");
  const canType = flagged("LicenseTypeMismatch");
  // The file opens only for a file issue (or a note-only return); a mismatch is fixed in its field.
  const canFile = fileRequired || me.correctionIssues.length === 0;

  const [file, setFile] = useState<File | null>(null);
  const [startDate, setStartDate] = useState(() => isoToHijriDate(me.issueDate));
  const [endDate, setEndDate] = useState(() => isoToHijriDate(me.expiryDate));
  const [licenseType, setLicenseType] = useState<LicenseType>(me.licenseType);
  const [licenseNumber, setLicenseNumber] = useState(me.licenseNumber);
  const [showErrors, setShowErrors] = useState(false);
  const [busy, setBusy] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const issueIso = hijriToIsoDate(startDate);
  const expiryIso = hijriToIsoDate(endDate);
  const errors = {
    file: file
      ? !ACCEPTED_TYPES.includes(file.type)
        ? t("lawyerAuth.register.errors.fileType")
        : file.size > MAX_FILE_BYTES
          ? t("lawyerAuth.register.errors.fileSize")
          : null
      : fileRequired
        ? t("lawyerReview.lawyer.newFileRequired")
        : null,
    startDate: !canDates || issueIso ? null : t("lawyerAuth.register.errors.date"),
    endDate: !canDates
      ? null
      : !expiryIso
        ? t("lawyerAuth.register.errors.date")
        : issueIso && expiryIso <= issueIso
          ? t("lawyerAuth.register.errors.endAfterStart")
          : null,
    licenseNumber: !canNumber || licenseNumber.trim() ? null : t("lawyerAuth.register.errors.required"),
  };
  const err = (message: string | null) => (showErrors ? message : null);

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (Object.values(errors).some(Boolean)) {
      setShowErrors(true);
      return;
    }
    setServerError(null);
    setBusy(true);
    try {
      await resubmitLicense({ licenseType, licenseNumber: licenseNumber.trim(), issueDate: issueIso!, expiryDate: expiryIso!, licenseDocument: file });
      // Back to "under review".
      await queryClient.invalidateQueries({ queryKey: ["lawyerMe"] });
    } catch {
      setServerError(t("lawyerReview.lawyer.resubmitFailed"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <Card className="border-rubric-tint">
        <div className="mb-3 flex items-center gap-2">
          <AlertTriangle className="h-5 w-5 text-rubric" />
          <p className="font-semibold text-ink">{t("lawyerReview.lawyer.changesTitle")}</p>
        </div>
        <p className="mb-2 text-sm text-ink-soft">{t("lawyerReview.lawyer.changesIntro")}</p>
        {me.correctionIssues.length > 0 && (
          <ul className="mb-2 list-disc ps-5 text-sm text-ink">
            {me.correctionIssues.map((issue) => (
              <li key={issue}>{t(`lawyerReview.issues.${issue}`)}</li>
            ))}
          </ul>
        )}
        {me.correctionNote && (
          <div className="mt-3 rounded-md bg-surface p-3">
            <p className="text-xs text-ink-faint">{t("lawyerReview.lawyer.adminNote")}</p>
            <p className="mt-1 whitespace-pre-line text-sm text-ink">{me.correctionNote}</p>
          </div>
        )}
      </Card>

      <form className="flex flex-col gap-4" onSubmit={submit} noValidate>
        <p className="font-semibold text-ink">{t("lawyerReview.lawyer.fixTitle")}</p>
        {canFile && (
          <>
            {me.documentFileName && !file && (
              <p className="text-xs text-ink-faint">{t("lawyerReview.lawyer.currentFile", { name: me.documentFileName })}</p>
            )}
            <LicenseUpload file={file} onChange={setFile} error={err(errors.file)} />
          </>
        )}
        {canDates && (
          <div className="grid grid-cols-2 gap-3">
            <Field label={t("lawyerAuth.register.startDate")} required error={err(errors.startDate)}>
              <HijriDateInput value={startDate} onChange={setStartDate} />
            </Field>
            <Field label={t("lawyerAuth.register.endDate")} required error={err(errors.endDate)}>
              <HijriDateInput value={endDate} onChange={setEndDate} align="end" />
            </Field>
          </div>
        )}
        {canType && (
          <Field label={t("lawyerAuth.register.licenseType")} required>
            <Select value={licenseType} onChange={(e) => setLicenseType(e.target.value as LicenseType)}>
              <option value="Licensed">{t("lawyerAuth.register.licenseTypes.Licensed")}</option>
              <option value="Trainee">{t("lawyerAuth.register.licenseTypes.Trainee")}</option>
            </Select>
          </Field>
        )}
        {canNumber && (
          <Field label={t("lawyerAuth.register.licenseNumber")} required error={err(errors.licenseNumber)}>
            <Input dir="ltr" className="font-mono" inputMode="numeric" value={licenseNumber} onChange={(e) => setLicenseNumber(e.target.value)} />
          </Field>
        )}

        {!(canFile && canDates && canType && canNumber) && (
          <div className="rounded-md border border-border bg-paper p-3">
            <p className="mb-2 flex items-center gap-1.5 text-xs font-medium text-ink-faint">
              <Lock className="h-3.5 w-3.5" />
              {t("lawyerReview.lawyer.unchangedTitle")}
            </p>
            <dl className="grid grid-cols-1 gap-2 text-sm sm:grid-cols-2">
              {!canFile && me.documentFileName && <Locked label={t("lawyerReview.lawyer.file")} value={me.documentFileName} />}
              {!canDates && <Locked label={t("lawyerAuth.register.startDate")} value={isoToHijriDate(me.issueDate)} mono />}
              {!canDates && <Locked label={t("lawyerAuth.register.endDate")} value={isoToHijriDate(me.expiryDate)} mono />}
              {!canType && <Locked label={t("lawyerAuth.register.licenseType")} value={t(`lawyerAuth.register.licenseTypes.${me.licenseType}`)} />}
              {!canNumber && <Locked label={t("lawyerAuth.register.licenseNumber")} value={me.licenseNumber} mono />}
            </dl>
          </div>
        )}
        {serverError && <p className="text-sm text-rubric">{serverError}</p>}
        <Button type="submit" className="w-full justify-center" disabled={busy}>
          {t("lawyerReview.lawyer.resubmit")}
        </Button>
        <div className="flex justify-center">
          <LogoutButton />
        </div>
      </form>
    </div>
  );
}

function Locked({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div>
      <dt className="text-xs text-ink-faint">{label}</dt>
      <dd className="text-ink">{mono ? <Ltr className="font-mono">{value}</Ltr> : value}</dd>
    </div>
  );
}
