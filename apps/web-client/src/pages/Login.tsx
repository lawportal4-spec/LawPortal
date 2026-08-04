import { useState, type FormEvent } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { Phone, ShieldCheck } from "lucide-react";
import { Button, Card, Input } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { useAuth } from "../lib/authContext";
import { requestClientOtp, verifyClientOtp } from "../lib/authApi";

export default function Login() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();

  const [step, setStep] = useState<"phone" | "code">("phone");
  const [localNumber, setLocalNumber] = useState("");
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const phoneE164 = `+966${localNumber.replace(/^0+/, "")}`;

  async function handleRequestOtp(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await requestClientOtp(phoneE164);
      setStep("code");
    } catch {
      setError(isAr ? "تعذّر إرسال الرمز. تحقّق من رقم الجوال." : "Could not send the code. Check the number.");
    } finally {
      setBusy(false);
    }
  }

  async function handleVerifyOtp(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const result = await verifyClientOtp(phoneE164, code);
      login(result);
      const from = (location.state as { from?: string } | null)?.from ?? "/orders";
      navigate(from, { replace: true });
    } catch {
      setError(isAr ? "رمز غير صحيح أو منتهي الصلاحية." : "That code is wrong or has expired.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <AppShell>
      <div className="mx-auto max-w-md">
        <h1 className="mb-1 font-display text-2xl font-bold">
          {isAr ? "تسجيل الدخول" : "Sign in"}
        </h1>
        <p className="mb-6 text-sm text-ink-faint">
          {isAr ? "بدون كلمة مرور — عبر رمز يصلك برسالة نصية." : "No password — we'll text you a one-time code."}
        </p>

        <Card>
          {step === "phone" ? (
            <form className="flex flex-col gap-4" onSubmit={handleRequestOtp}>
              <label className="text-sm font-medium text-ink-soft">
                {isAr ? "رقم الجوال" : "Mobile number"}
              </label>
              <div className="flex items-center gap-2">
                <span dir="ltr" className="rounded-md border border-border bg-surface-raised px-3 py-2.5 font-mono text-sm text-ink-faint">
                  +966
                </span>
                <Input
                  icon={<Phone className="h-4 w-4" />}
                  dir="ltr"
                  inputMode="numeric"
                  placeholder="5XXXXXXXX"
                  value={localNumber}
                  onChange={(e) => setLocalNumber(e.target.value.replace(/\D/g, ""))}
                  className="flex-1 font-mono"
                  required
                />
              </div>
              {error && <p className="text-sm text-rubric">{error}</p>}
              <Button type="submit" disabled={busy || localNumber.length < 9}>
                {busy ? (isAr ? "جارٍ الإرسال…" : "Sending…") : isAr ? "إرسال الرمز" : "Send code"}
              </Button>
            </form>
          ) : (
            <form className="flex flex-col gap-4" onSubmit={handleVerifyOtp}>
              <p className="text-sm text-ink-soft">
                {isAr ? "أدخل الرمز المرسل إلى" : "Enter the code sent to"}{" "}
                <bdi dir="ltr" className="font-mono">{phoneE164}</bdi>
              </p>
              <Input
                icon={<ShieldCheck className="h-4 w-4" />}
                dir="ltr"
                inputMode="numeric"
                placeholder="000000"
                value={code}
                onChange={(e) => setCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
                className="font-mono tracking-widest"
                maxLength={6}
                required
              />
              {error && <p className="text-sm text-rubric">{error}</p>}
              <Button type="submit" disabled={busy || code.length !== 6}>
                {busy ? (isAr ? "جارٍ التحقق…" : "Verifying…") : isAr ? "تأكيد الدخول" : "Verify & continue"}
              </Button>
              <button
                type="button"
                className="text-sm text-ink-faint hover:text-ink"
                onClick={() => setStep("phone")}
              >
                {isAr ? "تغيير الرقم" : "Change number"}
              </button>
            </form>
          )}
        </Card>
      </div>
    </AppShell>
  );
}
