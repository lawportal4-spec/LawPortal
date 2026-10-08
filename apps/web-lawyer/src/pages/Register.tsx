import { useEffect, useState, type FormEvent } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useNavigate } from "react-router-dom";
import { CircleCheck } from "lucide-react";
import { Button, Card, Input, Ltr, StepProgress } from "@law-portal/ui";
import { hijriToIsoDate, useTranslation } from "@law-portal/i18n";
import { AuthLayout, Field, PasswordInput, SaudiPhoneInput, Select } from "../components/AuthForm";
import { HijriDateInput } from "../components/HijriDateInput";
import { LicenseUpload } from "../components/LicenseUpload";
import { OtpCodeInput, ResendTimer } from "../components/OtpCodeInput";
import { ACCEPTED_TYPES, MAX_FILE_BYTES } from "../lib/licenseFile";
import { useCountdown } from "../lib/useCountdown";
import {
  getRegions,
  isValidNationalId,
  registerLawyer,
  resendLawyerRegistrationCode,
  toSaudiE164,
  verifyLawyerRegistration,
  type LicenseType,
} from "../lib/authApi";

const RESEND_SECONDS = 60;
const CODE_LENGTH = 6;
const MARKETING_URL = import.meta.env.VITE_MARKETING_URL ?? "http://localhost:5176";

type Step = 1 | 2 | 3 | "otp";

/**
 * Lawyer registration, mirroring the reference flow step for step:
 * personal data → region and city → licence → activation code → success → sign in.
 * Nothing is sent until step 3; the API then creates the account unactivated and texts a code.
 */
export default function Register() {
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const navigate = useNavigate();
  const regionsQuery = useQuery({ queryKey: ["regions"], queryFn: getRegions });

  const [step, setStep] = useState<Step>(1);
  const [showErrors, setShowErrors] = useState(false);
  const [busy, setBusy] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);
  const [registered, setRegistered] = useState(false);

  // Step 1
  const [fullName, setFullName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [nationalId, setNationalId] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  // Step 2
  const [regionId, setRegionId] = useState<number | null>(null);
  const [cityId, setCityId] = useState<number | null>(null);
  // Step 3
  const [file, setFile] = useState<File | null>(null);
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [licenseType, setLicenseType] = useState<LicenseType>("Licensed");
  const [licenseNumber, setLicenseNumber] = useState("");
  const [acceptedTerms, setAcceptedTerms] = useState(false);

  const phoneE164 = toSaudiE164(phone);
  const req = t("lawyerAuth.register.errors.required");
  const step1Errors = {
    fullName: fullName.trim() ? null : req,
    nationalId: !nationalId.trim() ? req : isValidNationalId(nationalId) ? null : t("lawyerAuth.register.errors.nationalId"),
    phone: phoneE164 ? null : t("lawyerAuth.register.errors.phone"),
    email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim()) ? null : t("lawyerAuth.register.errors.email"),
    password: password.length >= 8 && /[A-Z]/.test(password) && /\d/.test(password) ? null : t("lawyerAuth.register.errors.password"),
    confirmPassword: confirmPassword && confirmPassword === password ? null : t("lawyerAuth.register.errors.passwordMismatch"),
  };
  const step2Errors = { region: regionId ? null : req, city: cityId ? null : req };
  const issueIso = hijriToIsoDate(startDate);
  const expiryIso = hijriToIsoDate(endDate);
  const step3Errors = {
    file: !file
      ? req
      : !ACCEPTED_TYPES.includes(file.type)
        ? t("lawyerAuth.register.errors.fileType")
        : file.size > MAX_FILE_BYTES
          ? t("lawyerAuth.register.errors.fileSize")
          : null,
    startDate: issueIso ? null : t("lawyerAuth.register.errors.date"),
    endDate: !expiryIso
      ? t("lawyerAuth.register.errors.date")
      : issueIso && expiryIso <= issueIso
        ? t("lawyerAuth.register.errors.endAfterStart")
        : null,
    licenseNumber: licenseNumber.trim() ? null : req,
    terms: acceptedTerms ? null : t("lawyerAuth.register.errors.terms"),
  };
  const err = (message: string | null) => (showErrors ? message : null);
  const hasErrors = (errors: Record<string, string | null>) => Object.values(errors).some(Boolean);

  function goNext(e: FormEvent, errors: Record<string, string | null>, next: Step) {
    e.preventDefault();
    if (hasErrors(errors)) {
      setShowErrors(true);
      return;
    }
    setShowErrors(false);
    setStep(next);
  }

  async function submitRegistration(e: FormEvent) {
    e.preventDefault();
    if (hasErrors(step3Errors)) {
      setShowErrors(true);
      return;
    }
    setServerError(null);
    setBusy(true);
    try {
      await registerLawyer({
        fullName: fullName.trim(),
        phoneE164: phoneE164!,
        email: email.trim(),
        password,
        regionId: regionId!,
        cityId: cityId!,
        licenseType,
        licenseNumber: licenseNumber.trim(),
        nationalIdNumber: nationalId.trim(),
        issueDate: issueIso!,
        expiryDate: expiryIso!,
        countryCode: "SA",
        acceptedTerms,
        licenseDocument: file!,
      });
      setShowErrors(false);
      setStep("otp");
    } catch {
      setServerError(t("lawyerAuth.register.errors.submit"));
    } finally {
      setBusy(false);
    }
  }

  const selectedRegion = regionsQuery.data?.find((r) => r.id === regionId);
  const steps = [
    { label: t("lawyerAuth.register.steps.personal") },
    { label: t("lawyerAuth.register.steps.location") },
    { label: t("lawyerAuth.register.steps.license") },
  ];

  const haveAccount = (
    <p className="text-center text-sm text-ink-faint">
      {t("lawyerAuth.register.haveAccount")}{" "}
      <Link to="/login" className="font-semibold text-ink hover:text-seal">
        {t("lawyerAuth.register.signIn")}
      </Link>
    </p>
  );

  return (
    <AuthLayout title={t("lawyerAuth.register.title")}>
      {step !== "otp" && (
        <div className="mb-6">
          <StepProgress steps={steps} current={step} />
        </div>
      )}

      {step === 1 && (
        <form className="flex flex-col gap-4" onSubmit={(e) => goNext(e, step1Errors, 2)} noValidate>
          <Field label={t("lawyerAuth.register.fullName")} required error={err(step1Errors.fullName)}>
            <Input value={fullName} onChange={(e) => setFullName(e.target.value)} autoComplete="name" />
          </Field>
          <Field label={t("lawyerAuth.register.nationalId")} required error={err(step1Errors.nationalId)}>
            <Input dir="ltr" className="font-mono" inputMode="numeric" maxLength={10} value={nationalId} onChange={(e) => setNationalId(e.target.value)} />
          </Field>
          <Field label={t("lawyerAuth.register.phone")} required error={err(step1Errors.phone)}>
            <SaudiPhoneInput value={phone} onChange={(e) => setPhone(e.target.value)} />
          </Field>
          <Field label={t("lawyerAuth.register.email")} required error={err(step1Errors.email)}>
            <Input type="email" dir="ltr" className="font-mono" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" />
          </Field>
          <Field label={t("lawyerAuth.register.password")} required error={err(step1Errors.password)}>
            <PasswordInput value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" />
          </Field>
          <Field label={t("lawyerAuth.register.confirmPassword")} required error={err(step1Errors.confirmPassword)}>
            <PasswordInput value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} autoComplete="new-password" />
          </Field>
          <Button type="submit" className="mt-2 w-full justify-center">
            {t("lawyerAuth.register.next")}
          </Button>
          {haveAccount}
        </form>
      )}

      {step === 2 && (
        <form className="flex flex-col gap-4" onSubmit={(e) => goNext(e, step2Errors, 3)} noValidate>
          <Field label={t("lawyerAuth.register.region")} required error={err(step2Errors.region)}>
            <Select
              value={regionId ?? ""}
              onChange={(e) => {
                setRegionId(e.target.value ? Number(e.target.value) : null);
                setCityId(null);
              }}
            >
              <option value="">{t("lawyerAuth.register.select")}</option>
              {regionsQuery.data?.map((r) => (
                <option key={r.id} value={r.id}>
                  {isAr ? r.nameAr : r.nameEn}
                </option>
              ))}
            </Select>
          </Field>
          <Field label={t("lawyerAuth.register.city")} required error={err(step2Errors.city)}>
            <Select value={cityId ?? ""} onChange={(e) => setCityId(e.target.value ? Number(e.target.value) : null)} disabled={!selectedRegion}>
              <option value="">{t("lawyerAuth.register.select")}</option>
              {selectedRegion?.cities.map((c) => (
                <option key={c.id} value={c.id}>
                  {isAr ? c.nameAr : c.nameEn}
                </option>
              ))}
            </Select>
          </Field>
          <Button type="submit" className="mt-2 w-full justify-center">
            {t("lawyerAuth.register.next")}
          </Button>
          <Button type="button" variant="ghost" className="w-full justify-center" onClick={() => setStep(1)}>
            {t("lawyerAuth.register.back")}
          </Button>
          {haveAccount}
        </form>
      )}

      {step === 3 && (
        <form className="flex flex-col gap-4" onSubmit={submitRegistration} noValidate>
          <LicenseUpload file={file} onChange={setFile} error={err(step3Errors.file)} />
          <div className="grid grid-cols-2 gap-3">
            <Field label={t("lawyerAuth.register.startDate")} required error={err(step3Errors.startDate)}>
              <HijriDateInput value={startDate} onChange={setStartDate} />
            </Field>
            <Field label={t("lawyerAuth.register.endDate")} required error={err(step3Errors.endDate)}>
              <HijriDateInput value={endDate} onChange={setEndDate} align="end" />
            </Field>
          </div>
          <Field label={t("lawyerAuth.register.licenseType")} required>
            <Select value={licenseType} onChange={(e) => setLicenseType(e.target.value as LicenseType)}>
              <option value="Licensed">{t("lawyerAuth.register.licenseTypes.Licensed")}</option>
              <option value="Trainee">{t("lawyerAuth.register.licenseTypes.Trainee")}</option>
            </Select>
          </Field>
          <Field label={t("lawyerAuth.register.licenseNumber")} required error={err(step3Errors.licenseNumber)}>
            <Input dir="ltr" className="font-mono" inputMode="numeric" value={licenseNumber} onChange={(e) => setLicenseNumber(e.target.value)} />
          </Field>
          <Field label={t("lawyerAuth.register.country")} required>
            <Select value="SA" disabled>
              <option value="SA">{t("lawyerAuth.register.countries.SA")}</option>
            </Select>
          </Field>
          <div className="flex flex-col gap-1.5">
            <label className="flex items-center gap-2 text-sm text-ink-soft">
              <input type="checkbox" className="h-4 w-4 accent-seal" checked={acceptedTerms} onChange={(e) => setAcceptedTerms(e.target.checked)} />
              <span>
                {t("lawyerAuth.register.agreePrefix")}{" "}
                <a href={`${MARKETING_URL}${isAr ? "" : "/en"}/terms`} target="_blank" rel="noreferrer" className="font-semibold text-ink underline">
                  {t("lawyerAuth.register.terms")}
                </a>{" "}
                {t("lawyerAuth.register.and")}{" "}
                <a href={`${MARKETING_URL}${isAr ? "" : "/en"}/privacy`} target="_blank" rel="noreferrer" className="font-semibold text-ink underline">
                  {t("lawyerAuth.register.privacy")}
                </a>
              </span>
            </label>
            {err(step3Errors.terms) && <p className="text-xs text-rubric">{step3Errors.terms}</p>}
          </div>
          {serverError && <p className="text-sm text-rubric">{serverError}</p>}
          <Button type="submit" className="mt-2 w-full justify-center" disabled={busy}>
            {t("lawyerAuth.register.sendCode")}
          </Button>
          <Button type="button" variant="ghost" className="w-full justify-center" onClick={() => setStep(2)}>
            {t("lawyerAuth.register.back")}
          </Button>
        </form>
      )}

      {step === "otp" && phoneE164 && (
        <ActivationStep
          phoneE164={phoneE164}
          localPhone={phoneE164.slice(4)}
          onActivated={() => setRegistered(true)}
          onEditPhone={() => setStep(1)}
        />
      )}

      {registered && <SuccessDialog onDone={() => navigate("/login", { replace: true })} />}
    </AuthLayout>
  );
}

function ActivationStep({
  phoneE164,
  localPhone,
  onActivated,
  onEditPhone,
}: {
  phoneE164: string;
  localPhone: string;
  onActivated: () => void;
  onEditPhone: () => void;
}) {
  const { t } = useTranslation();
  const [digits, setDigits] = useState<string[]>(() => Array(CODE_LENGTH).fill(""));
  const [secondsLeft, restartCountdown] = useCountdown(RESEND_SECONDS);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function activate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await verifyLawyerRegistration(phoneE164, digits.join(""));
      onActivated();
    } catch {
      setError(t("lawyerAuth.otp.invalid"));
    } finally {
      setBusy(false);
    }
  }

  async function resend() {
    setError(null);
    try {
      await resendLawyerRegistrationCode(phoneE164);
      setDigits(Array(CODE_LENGTH).fill(""));
      restartCountdown();
    } catch {
      setError(t("lawyerAuth.otp.resendFailed"));
    }
  }

  return (
    <form className="flex flex-col items-center gap-4" onSubmit={activate}>
      <p className="text-sm text-ink-soft">{t("lawyerAuth.otp.sentTo")}</p>
      <Ltr className="font-mono text-base font-semibold text-ink">{localPhone}</Ltr>
      <OtpCodeInput digits={digits} onChange={setDigits} />
      <ResendTimer secondsLeft={secondsLeft} onResend={resend} />
      {error && <p className="text-sm text-rubric">{error}</p>}
      <Button type="submit" className="w-full max-w-xs justify-center" disabled={busy || digits.some((d) => !d)}>
        {t("lawyerAuth.otp.activate")}
      </Button>
      <button type="button" onClick={onEditPhone} className="text-sm text-seal hover:underline">
        {t("lawyerAuth.otp.editPhone")}
      </button>
    </form>
  );
}

function SuccessDialog({ onDone }: { onDone: () => void }) {
  const { t } = useTranslation();
  useEffect(() => {
    const timer = setTimeout(onDone, 3000);
    return () => clearTimeout(timer);
  }, [onDone]);
  return (
    <div role="dialog" aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4">
      <Card elevated className="flex w-full max-w-sm flex-col items-center gap-3 py-8 text-center">
        <CircleCheck className="h-12 w-12 text-seal" />
        <p className="text-base font-semibold text-ink">{t("lawyerAuth.success.title")}</p>
        <p className="text-sm text-ink-faint">{t("lawyerAuth.success.redirect")}</p>
      </Card>
    </div>
  );
}
