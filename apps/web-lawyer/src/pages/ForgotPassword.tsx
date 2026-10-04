import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { CircleCheck } from "lucide-react";
import { Button, Ltr } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AuthLayout, Field, PasswordInput, SaudiPhoneInput } from "../components/AuthForm";
import { OtpCodeInput, ResendTimer } from "../components/OtpCodeInput";
import { requestPasswordReset, resetPassword, toSaudiE164, verifyResetCode } from "../lib/authApi";
import { useCountdown } from "../lib/useCountdown";

const CODE_LENGTH = 6;
const RESEND_SECONDS = 60;
const isStrongPassword = (p: string) => p.length >= 8 && /[A-Z]/.test(p) && /\d/.test(p);

/** "Forgot password": mobile → code (verified on its own) → new password → back to sign-in. */
export default function ForgotPassword() {
  const { t } = useTranslation();
  const [phone, setPhone] = useState("");
  const [sentTo, setSentTo] = useState<string | null>(null);
  const [verifiedCode, setVerifiedCode] = useState<string | null>(null);
  const [done, setDone] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const phoneE164 = toSaudiE164(phone);

  async function sendCode(e: FormEvent) {
    e.preventDefault();
    if (!phoneE164) return setError(t("lawyerAuth.register.errors.phone"));
    setError(null);
    setBusy(true);
    try {
      await requestPasswordReset(phoneE164);
      setSentTo(phoneE164);
    } catch {
      setError(t("lawyerAuth.forgot.sendFailed"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthLayout title={t("lawyerAuth.forgot.title")}>
      {!sentTo && (
        <form className="flex flex-col gap-4" onSubmit={sendCode} noValidate>
          <p className="text-center text-sm text-ink-soft">{t("lawyerAuth.forgot.intro")}</p>
          <Field label={t("lawyerAuth.register.phone")} required error={error}>
            <SaudiPhoneInput value={phone} onChange={(e) => setPhone(e.target.value)} />
          </Field>
          <Button type="submit" className="mt-2 w-full justify-center" disabled={busy || !phone}>
            {t("lawyerAuth.forgot.sendCode")}
          </Button>
          <BackToLogin />
        </form>
      )}

      {sentTo && !verifiedCode && (
        <CodeStep phoneE164={sentTo} onVerified={setVerifiedCode} onChangeNumber={() => setSentTo(null)} />
      )}

      {sentTo && verifiedCode && !done && (
        <PasswordStep
          phoneE164={sentTo}
          code={verifiedCode}
          onDone={() => setDone(true)}
          onCodeRejected={() => setVerifiedCode(null)}
        />
      )}

      {done && <Done />}
    </AuthLayout>
  );
}

function BackToLogin() {
  const { t } = useTranslation();
  return (
    <p className="text-center text-sm">
      <Link to="/login" className="font-semibold text-ink hover:text-seal">
        {t("lawyerAuth.forgot.backToLogin")}
      </Link>
    </p>
  );
}

/** Step 2: the code alone — checked by the server before the password fields appear. */
function CodeStep({ phoneE164, onVerified, onChangeNumber }: { phoneE164: string; onVerified: (code: string) => void; onChangeNumber: () => void }) {
  const { t } = useTranslation();
  const [digits, setDigits] = useState<string[]>(() => Array(CODE_LENGTH).fill(""));
  const [secondsLeft, restartCountdown] = useCountdown(RESEND_SECONDS);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function verify(e: FormEvent) {
    e.preventDefault();
    const code = digits.join("");
    setError(null);
    setBusy(true);
    try {
      await verifyResetCode(phoneE164, code);
      onVerified(code);
    } catch {
      setError(t("lawyerAuth.forgot.failed"));
    } finally {
      setBusy(false);
    }
  }

  async function resend() {
    setError(null);
    await requestPasswordReset(phoneE164).catch(() => setError(t("lawyerAuth.forgot.sendFailed")));
    setDigits(Array(CODE_LENGTH).fill(""));
    restartCountdown();
  }

  return (
    <form className="flex flex-col items-center gap-4 text-center" onSubmit={verify}>
      <p className="text-sm text-ink-soft">{t("lawyerAuth.forgot.codeSent")}</p>
      <Ltr className="font-mono text-base font-semibold text-ink">{phoneE164.slice(4)}</Ltr>
      <OtpCodeInput digits={digits} onChange={setDigits} />
      <ResendTimer secondsLeft={secondsLeft} onResend={resend} />
      {error && <p className="text-sm text-rubric">{error}</p>}
      <Button type="submit" className="w-full max-w-xs justify-center" disabled={busy || digits.some((d) => !d)}>
        {t("lawyerAuth.forgot.verify")}
      </Button>
      <button type="button" onClick={onChangeNumber} className="text-sm text-seal hover:underline">
        {t("lawyerAuth.forgot.changeNumber")}
      </button>
    </form>
  );
}

/** Step 3: the new password. The verified code goes along with it — the server checks it again. */
function PasswordStep({
  phoneE164,
  code,
  onDone,
  onCodeRejected,
}: {
  phoneE164: string;
  code: string;
  onDone: () => void;
  onCodeRejected: () => void;
}) {
  const { t } = useTranslation();
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [showErrors, setShowErrors] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const passwordError = isStrongPassword(password) ? null : t("lawyerAuth.register.errors.password");
  const confirmError = confirm && confirm === password ? null : t("lawyerAuth.register.errors.passwordMismatch");

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (passwordError || confirmError) return setShowErrors(true);
    setError(null);
    setBusy(true);
    try {
      await resetPassword(phoneE164, code, password);
      onDone();
    } catch {
      // The only way a verified code fails here is that it expired meanwhile (5 minutes).
      setError(t("lawyerAuth.forgot.codeExpired"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="flex flex-col gap-4" onSubmit={submit} noValidate>
      <p className="text-center text-sm text-seal">{t("lawyerAuth.forgot.verified")}</p>
      <Field label={t("lawyerAuth.forgot.newPassword")} required error={showErrors ? passwordError : null}>
        <PasswordInput value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" />
      </Field>
      <Field label={t("lawyerAuth.forgot.confirmPassword")} required error={showErrors ? confirmError : null}>
        <PasswordInput value={confirm} onChange={(e) => setConfirm(e.target.value)} autoComplete="new-password" />
      </Field>
      {error && (
        <div className="flex flex-col items-center gap-2">
          <p className="text-sm text-rubric">{error}</p>
          <button type="button" onClick={onCodeRejected} className="text-sm text-seal hover:underline">
            {t("lawyerAuth.otp.resend")}
          </button>
        </div>
      )}
      <Button type="submit" className="mt-2 w-full justify-center" disabled={busy}>
        {t("lawyerAuth.forgot.submit")}
      </Button>
    </form>
  );
}

function Done() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  useEffect(() => {
    const timer = setTimeout(() => navigate("/login", { replace: true }), 3000);
    return () => clearTimeout(timer);
  }, [navigate]);
  return (
    <div className="flex flex-col items-center gap-3 text-center">
      <CircleCheck className="h-12 w-12 text-seal" />
      <p className="text-base font-semibold text-ink">{t("lawyerAuth.forgot.successTitle")}</p>
      <p className="text-sm text-ink-faint">{t("lawyerAuth.forgot.successRedirect")}</p>
      <BackToLogin />
    </div>
  );
}
